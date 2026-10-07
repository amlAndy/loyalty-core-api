using System.Net;
using System.Net.Http.Json;
using DotNetApiTemplate.Api.Contracts.Requests.Admin;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.Auth;

public sealed class AdminAssignmentAuthorizationTests
{
    [Fact]
    public async Task RoleUpdate_MetadataOnly_Allows_With_RolesUpdate()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.RolesUpdate]));

        var response = await client.PutAsJsonAsync("/api/admin/roles/role-1", new UpdateAdminRoleRequestDto
        {
            Name = "Role A",
            Description = "Updated description",
            ParentRoleId = "role-parent",
            Permissions = [AdminPermissionPolicies.UsersView, AdminPermissionPolicies.RolesView]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RoleUpdate_PermissionsChange_Requires_PermissionsAssign()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.RolesUpdate]));

        var response = await client.PutAsJsonAsync("/api/admin/roles/role-1", new UpdateAdminRoleRequestDto
        {
            Name = "Role A",
            Description = "Updated description",
            ParentRoleId = "role-parent",
            Permissions = [AdminPermissionPolicies.UsersView, AdminPermissionPolicies.UsersUpdate]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleUpdate_PermissionsChange_NonSelectedRolePermission_DoesNotGrantAccess()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.RolesUpdate]),
            new RoleSeed(ApplicationRoleNames.TownAdmin, [AdminPermissionPolicies.PermissionsAssign]));

        var response = await client.PutAsJsonAsync("/api/admin/roles/role-1", new UpdateAdminRoleRequestDto
        {
            Name = "Role A",
            Description = "Updated description",
            ParentRoleId = "role-parent",
            Permissions = [AdminPermissionPolicies.UsersView, AdminPermissionPolicies.UsersUpdate]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleUpdate_PermissionsChange_Allows_With_PermissionsAssign()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.RolesUpdate, AdminPermissionPolicies.PermissionsAssign]));

        var response = await client.PutAsJsonAsync("/api/admin/roles/role-1", new UpdateAdminRoleRequestDto
        {
            Name = "Role A",
            Description = "Updated description",
            ParentRoleId = "role-parent",
            Permissions = [AdminPermissionPolicies.UsersView, AdminPermissionPolicies.UsersUpdate]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserCreate_MetadataOnly_Allows_With_UsersCreate()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersCreate]));

        var response = await client.PostAsJsonAsync("/api/admin/users", new AdminUserUpsertDto
        {
            DisplayName = "Test User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = []
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserCreate_WithRoleAssignment_Requires_RolesAssign()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersCreate]));

        var response = await client.PostAsJsonAsync("/api/admin/users", new AdminUserUpsertDto
        {
            DisplayName = "Test User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = ["role-1"]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserCreate_WithRoleAssignment_Allows_With_RolesAssign()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersCreate, AdminPermissionPolicies.RolesAssign]));

        var response = await client.PostAsJsonAsync("/api/admin/users", new AdminUserUpsertDto
        {
            DisplayName = "Test User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = ["role-1"]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserCreate_RoleAssignment_NonSelectedRolePermission_DoesNotGrantAccess()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersCreate]),
            new RoleSeed(ApplicationRoleNames.TownAdmin, [AdminPermissionPolicies.RolesAssign]));

        var response = await client.PostAsJsonAsync("/api/admin/users", new AdminUserUpsertDto
        {
            DisplayName = "Test User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = ["role-1"]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserUpdate_MetadataOnly_Allows_With_UsersUpdate()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersUpdate]));

        var response = await client.PutAsJsonAsync("/api/admin/users/user-1", new AdminUserUpsertDto
        {
            DisplayName = "Updated User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = ["role-1"]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserUpdate_RoleAssignment_Requires_RolesAssign()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersUpdate]));

        var response = await client.PutAsJsonAsync("/api/admin/users/user-1", new AdminUserUpsertDto
        {
            DisplayName = "Updated User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = ["role-1", "role-2"]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserUpdate_RoleAssignment_Allows_With_RolesAssign()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersUpdate, AdminPermissionPolicies.RolesAssign]));

        var response = await client.PutAsJsonAsync("/api/admin/users/user-1", new AdminUserUpsertDto
        {
            DisplayName = "Updated User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = ["role-1", "role-2"]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserUpdate_RoleAssignment_NonSelectedRolePermission_DoesNotGrantAccess()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersUpdate]),
            new RoleSeed(ApplicationRoleNames.TownAdmin, [AdminPermissionPolicies.RolesAssign]));

        var response = await client.PutAsJsonAsync("/api/admin/users/user-1", new AdminUserUpsertDto
        {
            DisplayName = "Updated User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = ["role-1", "role-2"]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Inactive")]
    [InlineData("Locked")]
    [InlineData("PendingInvite")]
    public async Task UserUpdate_StatusChange_AwayFromActive_Requires_UsersDeactivate(string targetStatus)
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersUpdate]));

        var response = await client.PutAsJsonAsync("/api/admin/users/active-user", new AdminUserUpsertDto
        {
            DisplayName = "Updated User",
            Email = "test.user@example.com",
            Status = targetStatus,
            RoleIds = ["role-1"]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserUpdate_StatusChange_InactiveToActive_Requires_UsersActivate()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersUpdate]));

        var response = await client.PutAsJsonAsync("/api/admin/users/inactive-user", new AdminUserUpsertDto
        {
            DisplayName = "Updated User",
            Email = "test.user@example.com",
            Status = "Active",
            RoleIds = ["role-1"]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Inactive")]
    [InlineData("Locked")]
    [InlineData("PendingInvite")]
    public async Task UserCreate_NonActiveStatus_Requires_UsersDeactivate(string targetStatus)
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.PlatformAdmin,
            new RoleSeed(ApplicationRoleNames.PlatformAdmin, [AdminPermissionPolicies.UsersCreate]));

        var response = await client.PostAsJsonAsync("/api/admin/users", new AdminUserUpsertDto
        {
            DisplayName = "Test User",
            Email = "test.user@example.com",
            Status = targetStatus,
            RoleIds = []
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

