using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Services;
using DotNetApiTemplate.Application.Features.Admin.Roles.Validation;
using Xunit;

namespace DotNetApiTemplate.Application.Tests.Features.Admin.Roles.Services;

public sealed class AdminRoleServiceTests
{
    [Fact]
    public async Task SearchAsync_UsesDefaultPagingValues_WhenCriteriaPagingIsMissingOrInvalid()
    {
        var service = CreateService(
        [
            CreateRole("role-1", "Role 1"),
            CreateRole("role-2", "Role 2"),
            CreateRole("role-3", "Role 3")
        ]);

        var result = await service.SearchAsync(new AdminRoleSearchCriteria
        {
            Query = "   ",
            PageNumber = 0,
            PageSize = -5
        });

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Items.Length);
    }

    [Fact]
    public async Task SearchAsync_ReturnsRequestedPages_WithStableOrdering()
    {
        var service = CreateService(
        [
            CreateRole("role-gamma", "Gamma"),
            CreateRole("role-alpha", "Alpha"),
            CreateRole("role-beta", "Beta")
        ]);

        var firstPage = await service.SearchAsync(new AdminRoleSearchCriteria
        {
            PageNumber = 1,
            PageSize = 2
        });
        var secondPage = await service.SearchAsync(new AdminRoleSearchCriteria
        {
            PageNumber = 2,
            PageSize = 2
        });

        Assert.Equal(3, firstPage.Total);
        Assert.Equal(1, firstPage.PageNumber);
        Assert.Equal(2, firstPage.PageSize);
        Assert.Equal(["Alpha", "Beta"], firstPage.Items.Select(item => item.Name).ToArray());

        Assert.Equal(3, secondPage.Total);
        Assert.Equal(2, secondPage.PageNumber);
        Assert.Equal(2, secondPage.PageSize);
        Assert.Equal(["Gamma"], secondPage.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task SearchAsync_AppliesFilterBeforePaging_AndReturnsFilteredTotal()
    {
        AdminRoleDefinitionModel[] roles =
        [
            CreateRole("role-super-user", "Super User", isRootRole: true),
            CreateRole("role-administrator", "Administrator", parentRoleId: "role-super-user"),
            CreateRole("role-admin-assistant", "Admin Assistant", parentRoleId: "role-administrator"),
            CreateRole("role-admin-analyst", "Admin Analyst", parentRoleId: "role-administrator"),
            CreateRole("role-member", "Member", parentRoleId: "role-administrator"),
        ];
        var service = CreateService(roles);

        var result = await service.SearchAsync(new AdminRoleSearchCriteria
        {
            Query = "  admin ",
            PageNumber = 2,
            PageSize = 1
        });

        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal("Admin Assistant", result.Items[0].Name);
        Assert.Equal("Administrator", result.Items[0].ParentRoleName);
        Assert.Equal(0, result.Items[0].ChildCount);
        Assert.False(result.Items[0].IsProtected);
    }

    [Fact]
    public async Task CreateAsync_writes_role_created_audit_entry()
    {
        var parentRole = CreateRole("role-super-user", "Super User", isRootRole: true, permissions: ["roles.create"]);
        var createdRole = CreateRole("role-custom", "Custom Role", parentRoleId: "role-super-user", permissions: ["roles.create"]);
        var repository = new FakeAdminRoleRepository(
            getAllResults: [
                [parentRole],
                [parentRole, createdRole]
            ],
            createResult: createdRole);
        var auditWriter = new FakeAuditWriter();
        var service = CreateService(repository, auditWriter, new FakePermissionCatalogProvider("roles.create"));

        await service.CreateAsync(new AdminRoleUpsertModel
        {
            Name = "Custom Role",
            Description = "Custom Role description",
            ParentRoleId = "role-super-user",
            Permissions = ["roles.create"]
        });

        var entry = Assert.Single(auditWriter.Entries);
        Assert.Equal("RoleCreated", entry.Action);
        Assert.Equal("Custom Role", entry.TargetName);
    }

    [Fact]
    public async Task UpdateAsync_writes_role_updated_and_permission_delta_audit_entries()
    {
        var parentBefore = CreateRole("role-super-user", "Super User", isRootRole: true, permissions: ["roles.create", "roles.update"]);
        var parentAfter = CreateRole("role-super-user", "Super User", isRootRole: true, permissions: ["roles.create", "roles.update"]);
        var before = CreateRole("role-custom", "Custom Role", parentRoleId: "role-super-user", permissions: ["roles.create"]);
        var after = CreateRole("role-custom", "Custom Role Updated", parentRoleId: "role-super-user", permissions: ["roles.update"]);
        var repository = new FakeAdminRoleRepository(
            getAllResults: [
                [parentBefore, before],
                [parentAfter, after]
            ],
            updateResult: after);
        var auditWriter = new FakeAuditWriter();
        var service = CreateService(repository, auditWriter, new FakePermissionCatalogProvider("roles.create", "roles.update"));

        await service.UpdateAsync("role-custom", new AdminRoleUpsertModel
        {
            Name = "Custom Role Updated",
            Description = "Custom Role Updated description",
            ParentRoleId = "role-super-user",
            Permissions = ["roles.update"]
        });

        Assert.Contains(auditWriter.Entries, entry => entry.Action == "RoleUpdated");
        Assert.Contains(auditWriter.Entries, entry => entry.Action == "RolePermissionAssigned" && entry.Summary.Contains("roles.update", StringComparison.Ordinal));
        Assert.Contains(auditWriter.Entries, entry => entry.Action == "RolePermissionUnassigned" && entry.Summary.Contains("roles.create", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeleteAsync_writes_role_deleted_audit_entry()
    {
        var role = CreateRole("role-custom", "Custom Role", parentRoleId: "role-super-user", permissions: ["roles.create"]);
        var parentRole = CreateRole("role-super-user", "Super User", isRootRole: true, permissions: ["roles.create"]);
        var repository = new FakeAdminRoleRepository(
            getAllResults: [[parentRole, role]],
            deleteResult: true);
        var auditWriter = new FakeAuditWriter();
        var service = CreateService(repository, auditWriter, new FakePermissionCatalogProvider("roles.create"));

        var result = await service.DeleteAsync("role-custom");

        Assert.True(result);
        var entry = Assert.Single(auditWriter.Entries);
        Assert.Equal("RoleDeleted", entry.Action);
        Assert.Equal("Custom Role", entry.TargetName);
    }

    private static AdminRoleService CreateService(
        AdminRoleDefinitionModel[] roles,
        FakeAuditWriter? auditWriter = null,
        FakePermissionCatalogProvider? permissionCatalogProvider = null)
    {
        return CreateService(new FakeAdminRoleRepository(getAllResults: [roles]), auditWriter, permissionCatalogProvider);
    }

    private static AdminRoleService CreateService(
        FakeAdminRoleRepository repository,
        FakeAuditWriter? auditWriter = null,
        FakePermissionCatalogProvider? permissionCatalogProvider = null)
    {
        return new AdminRoleService(
            auditWriter ?? new FakeAuditWriter(),
            new ImmediateAdminMutationScope(),
            repository,
            new AdminRoleHierarchyPolicyService(),
            permissionCatalogProvider ?? new FakePermissionCatalogProvider(),
            new AdminRoleUpsertModelValidator(),
            new ValidateRolePermissionsModelValidator());
    }

    private static AdminRoleDefinitionModel CreateRole(
        string id,
        string name,
        string? parentRoleId = null,
        bool isRootRole = false,
        string[]? permissions = null)
    {
        var timestamp = DateTimeOffset.Parse("2026-04-20T09:00:00Z");

        return new AdminRoleDefinitionModel
        {
            Id = id,
            Name = name,
            Description = $"{name} description",
            ParentRoleId = parentRoleId,
            IsRootRole = isRootRole,
            IsSystemRole = isRootRole,
            Permissions = permissions ?? [],
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp
        };
    }

    private sealed class FakeAdminRoleRepository : IAdminRoleRepository
    {
        private readonly Queue<AdminRoleDefinitionModel[]> _getAllResults;
        private readonly AdminRoleDefinitionModel? _createResult;
        private readonly bool _deleteResult;
        private readonly AdminRoleDefinitionModel? _updateResult;

        public FakeAdminRoleRepository(
            AdminRoleDefinitionModel[][] getAllResults,
            AdminRoleDefinitionModel? createResult = null,
            AdminRoleDefinitionModel? updateResult = null,
            bool deleteResult = false)
        {
            _getAllResults = new Queue<AdminRoleDefinitionModel[]>(getAllResults);
            _createResult = createResult;
            _updateResult = updateResult;
            _deleteResult = deleteResult;
        }

        public Task<AdminRoleDefinitionModel[]> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var current = _getAllResults.Count > 1
                ? _getAllResults.Dequeue()
                : _getAllResults.Peek();

            return Task.FromResult(current);
        }

        public Task<AdminRoleDefinitionModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_getAllResults.Peek().FirstOrDefault(role => role.Id == id));
        }

        public Task<AdminRoleDefinitionModel> CreateAsync(AdminRoleUpsertModel model, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_createResult ?? throw new NotSupportedException());
        }

        public Task<AdminRoleDefinitionModel?> UpdateAsync(string id, AdminRoleUpsertModel model, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_updateResult);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_deleteResult);
        }
    }

    private sealed class FakePermissionCatalogProvider : IPermissionCatalogProvider
    {
        private readonly string[] _permissionCodes;

        public FakePermissionCatalogProvider(params string[] permissionCodes)
        {
            _permissionCodes = permissionCodes;
        }

        public AdminPermissionModel[] GetPermissions()
        {
            return _permissionCodes
                .Select(permissionCode => new AdminPermissionModel
                {
                    Code = permissionCode,
                    Description = permissionCode
                })
                .ToArray();
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

    private sealed class ImmediateAdminMutationScope : IAdminMutationScope
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            return operation(cancellationToken);
        }
    }
}
