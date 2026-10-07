using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.Auth.Exceptions;
using Xunit;

namespace DotNetApiTemplate.Application.Tests.Features.Admin.Authorization;

public sealed class AdminPrivilegeEscalationGuardTests
{
    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Allows_WhenPermissionsAndParentAreWithinAuthority()
    {
        var actorRole = Role("role-town", ["users.view", "users.update", "roles.update"]);
        var roles = new[]
        {
            actorRole,
            Role("role-child", ["users.view"], parentRoleId: "role-town")
        };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await guard.EnsureRoleMutationWithinAuthorityAsync(
            new AdminRoleUpsertModel
            {
                Name = "Child",
                Description = "Child",
                ParentRoleId = "role-town",
                Permissions = ["users.view"]
            },
            "role-child",
            roles);
    }

    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Denies_WhenPermissionIsOutsideAuthority()
    {
        var actorRole = Role("role-town", ["users.view", "roles.update"]);
        var roles = new[] { actorRole, Role("role-child", ["users.view"])};
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureRoleMutationWithinAuthorityAsync(
                new AdminRoleUpsertModel
                {
                    Name = "Child",
                    Description = "Child",
                    ParentRoleId = "role-town",
                    Permissions = ["users.view", "users.delete"]
                },
                "role-child",
                roles));
    }

    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Denies_WhenExistingRoleIsOutsideAuthority()
    {
        var actorRole = Role("role-town", ["users.view", "roles.update"]);
        var roles = new[]
        {
            actorRole,
            Role("role-high", ["users.delete"]),
            Role("role-child", ["users.view"])
        };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureRoleMutationWithinAuthorityAsync(
                new AdminRoleUpsertModel
                {
                    Name = "Child",
                    Description = "Child",
                    ParentRoleId = "role-town",
                    Permissions = ["users.view"]
                },
                "role-high",
                roles));
    }

    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Denies_WhenParentRoleIsOutsideAuthority()
    {
        var actorRole = Role("role-town", ["users.view", "roles.update"]);
        var roles = new[]
        {
            actorRole,
            Role("role-high", ["users.delete"]),
            Role("role-child", ["users.view"])
        };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureRoleMutationWithinAuthorityAsync(
                new AdminRoleUpsertModel
                {
                    Name = "Child",
                    Description = "Child",
                    ParentRoleId = "role-high",
                    Permissions = ["users.view"]
                },
                "role-child",
                roles));
    }

    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Denies_WhenNonSelectedStrongerRoleExists()
    {
        var actorRole = Role("role-town", ["users.view", "roles.update"]);
        var strongerRole = Role("role-platform", ["users.delete", "roles.assign"], isRootRole: true);
        var roles = new[]
        {
            actorRole,
            strongerRole,
            Role("role-child", ["users.view"])
        };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1", actorUserRoleIds: ["role-town", "role-platform"]);

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureRoleMutationWithinAuthorityAsync(
                new AdminRoleUpsertModel
                {
                    Name = "Child",
                    Description = "Child",
                    ParentRoleId = "role-town",
                    Permissions = ["users.view", "users.delete"]
                },
                "role-child",
                roles));
    }

    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Denies_WhenRootRoleIsModified()
    {
        var actorRole = Role("role-town", ["users.view", "roles.update"]);
        var rootRole = Role("role-platform", ["users.view", "users.delete"], isRootRole: true);
        var roles = new[] { actorRole, rootRole };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureRoleMutationWithinAuthorityAsync(
                new AdminRoleUpsertModel
                {
                    Name = "Platform",
                    Description = "Platform",
                    ParentRoleId = null,
                    Permissions = ["users.view"]
                },
                "role-platform",
                roles));
    }

    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Denies_WhenCurrentActiveRoleIsNotAssignedToActor()
    {
        var actorRole = Role("role-town", ["users.view", "roles.update"]);
        var roles = new[] { actorRole, Role("role-child", ["users.view"]) };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1", actorUserRoleIds: ["role-other"]);

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureRoleMutationWithinAuthorityAsync(
                new AdminRoleUpsertModel
                {
                    Name = "Child",
                    Description = "Child",
                    ParentRoleId = "role-town",
                    Permissions = ["users.view"]
                },
                "role-child",
                roles));
    }

    [Fact]
    public async Task EnsureUserRoleAssignmentsWithinAuthorityAsync_Allows_WhenRoleWithinAuthority()
    {
        var actorRole = Role("role-town", ["users.view", "roles.assign"]);
        var memberRole = Role("role-member", []);
        var roles = new[] { actorRole, memberRole };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await guard.EnsureUserRoleAssignmentsWithinAuthorityAsync([], ["role-member"]);
    }

    [Fact]
    public async Task EnsureUserRoleAssignmentsWithinAuthorityAsync_Denies_WhenRoleExceedsAuthority()
    {
        var actorRole = Role("role-town", ["users.view", "roles.assign"]);
        var roles = new[] { actorRole, Role("role-platform", ["users.delete"], isRootRole: true) };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureUserRoleAssignmentsWithinAuthorityAsync([], ["role-platform"]));
    }

    [Fact]
    public async Task EnsureUserRoleAssignmentsWithinAuthorityAsync_Denies_WhenActorHasStrongerNonSelectedRole()
    {
        var actorRole = Role("role-town", ["users.view", "roles.assign"]);
        var strongerRole = Role("role-platform", ["users.delete"], isRootRole: true);
        var roles = new[] { actorRole, strongerRole };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1", actorUserRoleIds: ["role-town", "role-platform"]);

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureUserRoleAssignmentsWithinAuthorityAsync([], ["role-platform"]));
    }

    [Fact]
    public async Task EnsureUserRoleAssignmentsWithinAuthorityAsync_Denies_WhenTownAdminAssignsPlatformAdmin()
    {
        var actorRole = Role("role-town", ["users.view", "roles.assign"]);
        var platformRole = Role("role-platform", ["users.view", "users.delete"], isRootRole: true);
        var roles = new[] { actorRole, platformRole };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureUserRoleAssignmentsWithinAuthorityAsync([], ["role-platform"]));
    }

    [Fact]
    public async Task EnsureUserRoleAssignmentsWithinAuthorityAsync_Denies_WhenSelfEscalatingToStrongerRole()
    {
        var actorRole = Role("role-town", ["users.view", "roles.assign"]);
        var platformRole = Role("role-platform", ["users.view", "users.delete"], isRootRole: true);
        var roles = new[] { actorRole, platformRole };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureUserRoleAssignmentsWithinAuthorityAsync(["role-town"], ["role-platform"]));
    }

    [Fact]
    public async Task EnsureUserRoleAssignmentsWithinAuthorityAsync_Denies_WhenRemovingAboveAuthorityRole()
    {
        var actorRole = Role("role-town", ["users.view", "roles.assign"]);
        var platformRole = Role("role-platform", ["users.view", "users.delete"], isRootRole: true);
        var roles = new[] { actorRole, platformRole };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureUserRoleAssignmentsWithinAuthorityAsync(["role-town", "role-platform"], ["role-town"]));
    }

    [Fact]
    public async Task EnsureUserRoleAssignmentsWithinAuthorityAsync_Allows_WhenRemovingWithinAuthorityRole()
    {
        var actorRole = Role("role-town", ["users.view", "roles.assign"]);
        var memberRole = Role("role-member", []);
        var roles = new[] { actorRole, memberRole };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await guard.EnsureUserRoleAssignmentsWithinAuthorityAsync(["role-town", "role-member"], ["role-town"]);
    }

    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Allows_WhenParentChangeStaysWithinAuthority()
    {
        var actorRole = Role("role-town", ["users.view", "roles.update"]);
        var roles = new[]
        {
            actorRole,
            Role("role-child", ["users.view"])
        };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await guard.EnsureRoleMutationWithinAuthorityAsync(
            new AdminRoleUpsertModel
            {
                Name = "Child",
                Description = "Child",
                ParentRoleId = "role-town",
                Permissions = ["users.view"]
            },
            "role-child",
            roles);
    }

    [Fact]
    public async Task EnsureRoleMutationWithinAuthorityAsync_Denies_WhenParentChangeIntroducesAuthorityBeyondActor()
    {
        var actorRole = Role("role-town", ["users.view", "roles.update"]);
        var strongerParent = Role("role-platform", ["users.delete"], isRootRole: true);
        var roles = new[]
        {
            actorRole,
            strongerParent,
            Role("role-child", ["users.view"])
        };
        var guard = CreateGuard(roles, actorRole, actorUserId: "user-1");

        await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            guard.EnsureRoleMutationWithinAuthorityAsync(
                new AdminRoleUpsertModel
                {
                    Name = "Child",
                    Description = "Child",
                    ParentRoleId = "role-platform",
                    Permissions = ["users.view"]
                },
                "role-child",
                roles));
    }

    private static AdminPrivilegeEscalationGuard CreateGuard(
        IReadOnlyCollection<AdminRoleDefinitionModel> roles,
        AdminRoleDefinitionModel actorRole,
        string actorUserId,
        string[]? actorUserRoleIds = null)
    {
        return new AdminPrivilegeEscalationGuard(
            new FakeAdminActorContext(actorUserId, actorRole.Id),
            new FakeAdminRoleRepository(roles),
            new FakeAdminUserRepository(actorUserId, actorUserRoleIds ?? [actorRole.Id]));
    }

    private static AdminRoleDefinitionModel Role(
        string id,
        string[] permissions,
        string? parentRoleId = null,
        bool isRootRole = false,
        bool isSystemRole = false)
    {
        return new AdminRoleDefinitionModel
        {
            Id = id,
            Name = id,
            Description = id,
            ParentRoleId = parentRoleId,
            IsRootRole = isRootRole,
            IsSystemRole = isSystemRole,
            Permissions = permissions,
            CreatedAtUtc = DateTimeOffset.Parse("2026-04-20T09:00:00Z"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-04-20T09:00:00Z")
        };
    }

    private sealed class FakeAdminActorContext : IAdminActorContext
    {
        public FakeAdminActorContext(string? userId, string? activeRoleId)
        {
            UserId = userId;
            ActiveRoleId = activeRoleId;
        }

        public string? UserId { get; }
        public string? ActiveRoleId { get; }
    }

    private sealed class FakeAdminRoleRepository : IAdminRoleRepository
    {
        private readonly IReadOnlyCollection<AdminRoleDefinitionModel> _roles;

        public FakeAdminRoleRepository(IReadOnlyCollection<AdminRoleDefinitionModel> roles)
        {
            _roles = roles;
        }

        public Task<AdminRoleDefinitionModel[]> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_roles.ToArray());
        }

        public Task<AdminRoleDefinitionModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_roles.FirstOrDefault(role => string.Equals(role.Id, id, StringComparison.Ordinal)));
        }

        public Task<AdminRoleDefinitionModel> CreateAsync(AdminRoleUpsertModel model, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AdminRoleDefinitionModel?> UpdateAsync(string id, AdminRoleUpsertModel model, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeAdminUserRepository : IAdminUserRepository
    {
        private readonly AdminUserDetailModel? _actorUser;

        public FakeAdminUserRepository(string actorUserId, string[] actorUserRoleIds)
        {
            _actorUser = new AdminUserDetailModel
            {
                Id = actorUserId,
                DisplayName = actorUserId,
                Email = actorUserId,
                Status = "Active",
                RoleIds = actorUserRoleIds,
                Roles = actorUserRoleIds
            };
        }

        public Task<AdminUserDetailModel> CreateAsync(AdminUserUpsertModel model, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> EmailExistsAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<string[]> GetExistingRoleIdsAsync(string[] roleIds, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AdminUserFormOptionsModel> GetFormOptionsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AdminUserDetailModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actorUser is not null && string.Equals(_actorUser.Id, id, StringComparison.Ordinal)
                ? _actorUser
                : null);
        }

        public Task<AdminUserSearchResult> SearchUsersAsync(AdminUserSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AdminUserDetailModel?> UpdateAsync(string id, AdminUserUpsertModel model, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
