using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Users.Services;
using DotNetApiTemplate.Application.Features.Admin.Users.Validation;
using FluentValidation;
using Xunit;

namespace DotNetApiTemplate.Application.Tests.Features.Admin.Users.Services;

public sealed class AdminUserServiceTests
{
    [Fact]
    public async Task DeleteAsync_returns_true_when_repository_deletes_user()
    {
        var repository = new FakeAdminUserRepository(
            deleteResult: true,
            getByIdResult: new AdminUserDetailModel
            {
                Id = "user-1",
                DisplayName = "User 1",
                Email = "user1@example.com",
                Status = "Active",
                RoleIds = [],
                Roles = []
            });
        var service = CreateService(repository);

        var result = await service.DeleteAsync("user-1");

        Assert.True(result);
        Assert.Equal("user-1", repository.LastId);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_blank_id()
    {
        var repository = new FakeAdminUserRepository(deleteResult: true);
        var service = CreateService(repository);

        var result = await service.DeleteAsync(" ");

        Assert.False(result);
        Assert.Null(repository.LastId);
    }

    [Fact]
    public async Task CreateAsync_returns_created_user()
    {
        var expected = new AdminUserDetailModel
        {
            Id = "user-1",
            DisplayName = "New User",
            Email = "new@example.com",
            Status = "Active",
            RoleIds = ["member"],
            Roles = ["Member"]
        };
        var repository = new FakeAdminUserRepository(detailResult: expected, existingRoleIds: ["member"]);
        var service = CreateService(repository);

        var result = await service.CreateAsync(new AdminUserUpsertModel
        {
            DisplayName = "New User",
            Email = "new@example.com",
            Status = "Active",
            RoleIds = ["member"]
        });

        Assert.Same(expected, result);
        Assert.NotNull(repository.LastUpsertModel);
        Assert.Equal("new@example.com", repository.LastUpsertModel!.Email);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_email()
    {
        var repository = new FakeAdminUserRepository(emailExists: true, existingRoleIds: ["member"]);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new AdminUserUpsertModel
        {
            DisplayName = "New User",
            Email = "duplicate@example.com",
            Status = "Active",
            RoleIds = ["member"]
        }));
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_role_ids()
    {
        var repository = new FakeAdminUserRepository(existingRoleIds: ["member"]);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new AdminUserUpsertModel
        {
            DisplayName = "New User",
            Email = "new@example.com",
            Status = "Active",
            RoleIds = ["member", "missing"]
        }));
    }

    [Fact]
    public async Task GetFormOptionsAsync_returns_repository_result()
    {
        var expected = new AdminUserFormOptionsModel
        {
            Statuses = ["Active", "Inactive", "Locked", "PendingInvite"],
            Roles =
            [
                new AdminRoleOptionModel
                {
                    Id = "admin",
                    Name = "Admin",
                    ParentRoleId = null,
                    IsRootRole = true,
                    IsSystemRole = true
                }
            ]
        };
        var repository = new FakeAdminUserRepository(formOptionsResult: expected);
        var service = CreateService(repository);

        var result = await service.GetFormOptionsAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_returns_repository_result_when_found()
    {
        var expected = new AdminUserDetailModel
        {
            Id = "user-1",
            DisplayName = "Admin User",
            Email = "admin@example.com",
            Status = "Active",
            RoleIds = ["admin"],
            Roles = ["Admin"]
        };
        var repository = new FakeAdminUserRepository(detailResult: expected);
        var service = CreateService(repository);

        var result = await service.GetByIdAsync("user-1");

        Assert.Same(expected, result);
        Assert.Equal("user-1", repository.LastId);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_blank_id()
    {
        var repository = new FakeAdminUserRepository();
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(" ");

        Assert.Null(result);
        Assert.Null(repository.LastId);
    }

    [Fact]
    public async Task SearchUsersAsync_returns_repository_result()
    {
        var expected = new AdminUserSearchResult
        {
            Items =
            [
                new AdminUserSummaryModel
                {
                    Id = "user-1",
                    DisplayName = "Admin User",
                    Email = "admin@example.com",
                    Status = "Active",
                    RoleIds = ["admin"],
                    Roles = ["Admin"]
                }
            ],
            Total = 1,
            PageNumber = 1,
            PageSize = 10
        };
        var repository = new FakeAdminUserRepository(expected);
        var service = CreateService(repository);
        var criteria = new AdminUserSearchCriteria
        {
            Query = "admin",
            RoleId = "admin",
            Status = "Active",
            PageNumber = 2,
            PageSize = 25
        };

        var result = await service.SearchUsersAsync(criteria);

        Assert.Same(expected, result);
        Assert.NotNull(repository.LastCriteria);
        Assert.Equal("admin", repository.LastCriteria!.Query);
        Assert.Equal("admin", repository.LastCriteria.RoleId);
        Assert.Equal("Active", repository.LastCriteria.Status);
        Assert.Equal(2, repository.LastCriteria.PageNumber);
        Assert.Equal(25, repository.LastCriteria.PageSize);
    }

    [Fact]
    public async Task SearchUsersAsync_defaults_invalid_page_values()
    {
        var repository = new FakeAdminUserRepository(new AdminUserSearchResult());
        var service = CreateService(repository);

        await service.SearchUsersAsync(new AdminUserSearchCriteria
        {
            PageNumber = 0,
            PageSize = -5
        });

        Assert.NotNull(repository.LastCriteria);
        Assert.Equal(1, repository.LastCriteria!.PageNumber);
        Assert.Equal(10, repository.LastCriteria.PageSize);
    }

    [Fact]
    public async Task UpdateAsync_returns_updated_user_when_found()
    {
        var expected = new AdminUserDetailModel
        {
            Id = "user-1",
            DisplayName = "Updated User",
            Email = "updated@example.com",
            Status = "Inactive",
            RoleIds = ["supplier"],
            Roles = ["Supplier"]
        };
        var repository = new FakeAdminUserRepository(detailResult: expected, existingRoleIds: ["supplier"]);
        var service = CreateService(repository);

        var result = await service.UpdateAsync("user-1", new AdminUserUpsertModel
        {
            DisplayName = "Updated User",
            Email = "updated@example.com",
            Status = "Inactive",
            RoleIds = ["supplier"]
        });

        Assert.Same(expected, result);
        Assert.Equal("user-1", repository.LastId);
        Assert.NotNull(repository.LastUpsertModel);
        Assert.Equal("updated@example.com", repository.LastUpsertModel!.Email);
    }

    [Fact]
    public async Task UpdateAsync_returns_null_for_blank_id()
    {
        var repository = new FakeAdminUserRepository(existingRoleIds: ["member"]);
        var service = CreateService(repository);

        var result = await service.UpdateAsync(" ", new AdminUserUpsertModel
        {
            DisplayName = "User",
            Email = "user@example.com",
            Status = "Active",
            RoleIds = ["member"]
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_writes_user_created_audit_entry()
    {
        var createdUser = new AdminUserDetailModel
        {
            Id = "user-1",
            DisplayName = "New User",
            Email = "new@example.com",
            Status = "Active",
            RoleIds = ["member"],
            Roles = ["Member"]
        };
        var repository = new FakeAdminUserRepository(createResult: createdUser, existingRoleIds: ["member"]);
        var auditWriter = new FakeAuditWriter();
        var service = CreateService(repository, auditWriter);

        await service.CreateAsync(new AdminUserUpsertModel
        {
            DisplayName = "New User",
            Email = "new@example.com",
            Status = "Active",
            RoleIds = ["member"]
        });

        var entry = Assert.Single(auditWriter.Entries);
        Assert.Equal("UserCreated", entry.Action);
        Assert.Equal("New User", entry.TargetName);
        Assert.Contains(entry.Details, detail => detail.Field == "roles" && Assert.IsType<string[]>(detail.AfterValue).SequenceEqual(["Member"]));
    }

    [Fact]
    public async Task UpdateAsync_writes_status_and_role_audit_entries()
    {
        var before = new AdminUserDetailModel
        {
            Id = "user-1",
            DisplayName = "Admin User",
            Email = "admin@example.com",
            Status = "Inactive",
            RoleIds = ["member"],
            Roles = ["Member"]
        };
        var after = new AdminUserDetailModel
        {
            Id = "user-1",
            DisplayName = "Admin User",
            Email = "admin@example.com",
            Status = "Active",
            RoleIds = ["administrator"],
            Roles = ["Administrator"]
        };
        var repository = new FakeAdminUserRepository(getByIdResult: before, updateResult: after, existingRoleIds: ["administrator"]);
        var auditWriter = new FakeAuditWriter();
        var service = CreateService(repository, auditWriter);

        await service.UpdateAsync("user-1", new AdminUserUpsertModel
        {
            DisplayName = "Admin User",
            Email = "admin@example.com",
            Status = "Active",
            RoleIds = ["administrator"]
        });

        Assert.Contains(auditWriter.Entries, entry => entry.Action == "UserUpdated");
        Assert.Contains(auditWriter.Entries, entry => entry.Action == "UserStatusChanged");
        Assert.Contains(auditWriter.Entries, entry => entry.Action == "UserRoleAssigned" && entry.Summary.Contains("Administrator", StringComparison.Ordinal));
        Assert.Contains(auditWriter.Entries, entry => entry.Action == "UserRoleUnassigned" && entry.Summary.Contains("Member", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeleteAsync_writes_user_deleted_audit_entry()
    {
        var existingUser = new AdminUserDetailModel
        {
            Id = "user-1",
            DisplayName = "Admin User",
            Email = "admin@example.com",
            Status = "Active",
            RoleIds = ["administrator"],
            Roles = ["Administrator"]
        };
        var repository = new FakeAdminUserRepository(getByIdResult: existingUser, deleteResult: true);
        var auditWriter = new FakeAuditWriter();
        var service = CreateService(repository, auditWriter);

        var result = await service.DeleteAsync("user-1");

        Assert.True(result);
        var entry = Assert.Single(auditWriter.Entries);
        Assert.Equal("UserDeleted", entry.Action);
        Assert.Equal("Admin User", entry.TargetName);
    }

    private static AdminUserService CreateService(FakeAdminUserRepository repository, FakeAuditWriter? auditWriter = null)
    {
        return new AdminUserService(
            auditWriter ?? new FakeAuditWriter(),
            new ImmediateAdminMutationScope(),
            new AllowAllAdminPrivilegeEscalationGuard(),
            repository,
            new AdminUserUpsertModelValidator());
    }

    private sealed class FakeAdminUserRepository : IAdminUserRepository
    {
        private readonly AdminUserSearchResult _result;
        private readonly AdminUserDetailModel? _createResult;
        private readonly AdminUserFormOptionsModel _formOptionsResult;
        private readonly bool _emailExists;
        private readonly bool _deleteResult;
        private readonly string[] _existingRoleIds;
        private readonly AdminUserDetailModel? _getByIdResult;
        private readonly AdminUserDetailModel? _updateResult;

        public FakeAdminUserRepository(
            AdminUserSearchResult? result = null,
            AdminUserDetailModel? detailResult = null,
            AdminUserDetailModel? createResult = null,
            AdminUserFormOptionsModel? formOptionsResult = null,
            bool emailExists = false,
            bool deleteResult = false,
            string[]? existingRoleIds = null,
            AdminUserDetailModel? getByIdResult = null,
            AdminUserDetailModel? updateResult = null)
        {
            _result = result ?? new AdminUserSearchResult();
            _createResult = createResult ?? detailResult;
            _formOptionsResult = formOptionsResult ?? new AdminUserFormOptionsModel();
            _emailExists = emailExists;
            _deleteResult = deleteResult;
            _existingRoleIds = existingRoleIds ?? [];
            _getByIdResult = getByIdResult ?? detailResult;
            _updateResult = updateResult ?? detailResult;
        }

        public AdminUserSearchCriteria? LastCriteria { get; private set; }
        public string? LastId { get; private set; }
        public AdminUserUpsertModel? LastUpsertModel { get; private set; }

        public Task<AdminUserDetailModel> CreateAsync(AdminUserUpsertModel model, CancellationToken cancellationToken = default)
        {
            LastUpsertModel = model;
            return Task.FromResult(_createResult ?? new AdminUserDetailModel());
        }

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            LastId = id;
            return Task.FromResult(_deleteResult);
        }

        public Task<bool> EmailExistsAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_emailExists);
        }

        public Task<string[]> GetExistingRoleIdsAsync(string[] roleIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_existingRoleIds);
        }

        public Task<AdminUserFormOptionsModel> GetFormOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_formOptionsResult);
        }

        public Task<AdminUserDetailModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            LastId = id;
            return Task.FromResult(_getByIdResult);
        }

        public Task<AdminUserSearchResult> SearchUsersAsync(AdminUserSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(_result);
        }

        public Task<AdminUserDetailModel?> UpdateAsync(string id, AdminUserUpsertModel model, CancellationToken cancellationToken = default)
        {
            LastId = id;
            LastUpsertModel = model;
            return Task.FromResult(_updateResult);
        }
    }

    private sealed class FakeAuditWriter : IAuditWriter
    {
        public List<AuditWriteEntryModel> Entries { get; } = [];

        public Task WriteAsync(AuditWriteEntryModel entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class AllowAllAdminPrivilegeEscalationGuard : IAdminPrivilegeEscalationGuard
    {
        public Task EnsureRoleMutationWithinAuthorityAsync(AdminRoleUpsertModel proposedModel, string? existingRoleId, IReadOnlyCollection<AdminRoleDefinitionModel> roles, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task EnsureUserRoleAssignmentsWithinAuthorityAsync(string[] existingRoleIds, string[] requestedRoleIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ImmediateAdminMutationScope : IAdminMutationScope
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            return operation(cancellationToken);
        }
    }
}
