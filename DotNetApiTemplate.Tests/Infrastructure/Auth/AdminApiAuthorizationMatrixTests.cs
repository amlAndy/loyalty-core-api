using System.Net;
using System.Net.Http.Json;
using DotNetApiTemplate.Api.Contracts.Requests.Admin;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.Auth;

public sealed class AdminApiAuthorizationMatrixTests
{
    [Fact]
    public async Task RepresentativeAdminRoutes_Allows_When_SelectedActiveRole_HasRequiredPermission()
    {
        await using var factory = new AdminApiFactory();

        foreach (var routeCase in GetRouteCases())
        {
            using var client = await factory.CreateAuthenticatedClientAsync(
                ApplicationRoleNames.PlatformAdmin,
                new RoleSeed(ApplicationRoleNames.PlatformAdmin, [routeCase.RequiredPermission]));

            var response = await SendAsync(client, routeCase);

            Assert.True(routeCase.AllowedStatus == response.StatusCode, $"{routeCase.Name}: expected {routeCase.AllowedStatus}, got {response.StatusCode}");

        }
    }

    [Fact]
    public async Task RepresentativeAdminRoutes_Denies_When_SelectedActiveRole_LacksRequiredPermission()
    {
        await using var factory = new AdminApiFactory();

        foreach (var routeCase in GetRouteCases())
        {
            using var client = await factory.CreateAuthenticatedClientAsync(
                ApplicationRoleNames.PlatformAdmin,
                new RoleSeed(ApplicationRoleNames.PlatformAdmin, []),
                new RoleSeed(ApplicationRoleNames.TownAdmin, [routeCase.RequiredPermission]));

            var response = await SendAsync(client, routeCase);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task ProvisioningRoutes_Deny_BusinessOwnerEvenWithAdminConfigureClaim()
    {
        await using var factory = new AdminApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(
            ApplicationRoleNames.BusinessOwner,
            new RoleSeed(ApplicationRoleNames.BusinessOwner, [AdminPermissionPolicies.AdminConfigure]));

        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/access",
            new CreateBusinessStaffAccessProvisioningRequest(
                Guid.Parse("00000000-0000-0000-0000-000000000020"),
                "owner-user",
                "Owner Provision Attempt",
                "Staff",
                "Suspended",
                "Should be denied"));

        var updateResponse = await client.PutAsJsonAsync(
            "/api/admin/access/00000000-0000-0000-0000-000000000021/provisioning",
            new UpdateBusinessStaffAccessProvisioningRequest(
                "owner-user",
                "Active",
                "Should be denied"));

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    public static IEnumerable<object[]> AllowedCases()
    {
        yield return [new RouteMatrixCase("Users detail", HttpMethod.Get, "/api/admin/users/user-1", null, AdminPermissionPolicies.UsersView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Users create", HttpMethod.Post, "/api/admin/users", new AdminUserUpsertDto { DisplayName = "Test User", Email = "test.user@example.com", Status = "Active", RoleIds = [] }, AdminPermissionPolicies.UsersCreate, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Users update", HttpMethod.Put, "/api/admin/users/user-1", new AdminUserUpsertDto { DisplayName = "Test User", Email = "test.user@example.com", Status = "Active", RoleIds = ["role-1"] }, AdminPermissionPolicies.UsersUpdate, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Users delete", HttpMethod.Delete, "/api/admin/users/user-1", null, AdminPermissionPolicies.UsersDelete, HttpStatusCode.NoContent)];
        yield return [new RouteMatrixCase("Users form-options", HttpMethod.Get, "/api/admin/users/form-options", null, AdminPermissionPolicies.UsersView, HttpStatusCode.OK)];

        yield return [new RouteMatrixCase("Roles detail", HttpMethod.Get, "/api/admin/roles/role-1", null, AdminPermissionPolicies.RolesView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Roles create", HttpMethod.Post, "/api/admin/roles", new CreateAdminRoleRequestDto { Name = "Role A", Description = "Role A", ParentRoleId = "role-parent", Permissions = [] }, AdminPermissionPolicies.RolesCreate, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Roles update", HttpMethod.Put, "/api/admin/roles/role-1", new UpdateAdminRoleRequestDto { Name = "Role A", Description = "Role A", ParentRoleId = "role-parent", Permissions = [AdminPermissionPolicies.UsersView, AdminPermissionPolicies.RolesView] }, AdminPermissionPolicies.RolesUpdate, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Roles delete", HttpMethod.Delete, "/api/admin/roles/role-1", null, AdminPermissionPolicies.RolesDelete, HttpStatusCode.NoContent)];
        yield return [new RouteMatrixCase("Roles form-options", HttpMethod.Get, "/api/admin/roles/form-options", null, AdminPermissionPolicies.RolesView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Roles parent-options", HttpMethod.Get, "/api/admin/roles/parent-options", null, AdminPermissionPolicies.RolesView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Roles available-permissions", HttpMethod.Get, "/api/admin/roles/available-permissions?parentRoleId=role-parent", null, AdminPermissionPolicies.PermissionsView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Roles validate-permissions", HttpMethod.Post, "/api/admin/roles/validate-permissions", new ValidateRolePermissionsRequestDto { CurrentRoleId = "role-1", ParentRoleId = "role-parent", Permissions = [AdminPermissionPolicies.UsersView] }, AdminPermissionPolicies.PermissionsView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Roles delete-eligibility", HttpMethod.Get, "/api/admin/roles/role-1/delete-eligibility", null, AdminPermissionPolicies.RolesView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Roles children", HttpMethod.Get, "/api/admin/roles/role-1/children", null, AdminPermissionPolicies.RolesView, HttpStatusCode.OK)];

        yield return [new RouteMatrixCase("Audit search", HttpMethod.Get, "/api/admin/audit", null, AdminPermissionPolicies.AuditView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Audit filters", HttpMethod.Get, "/api/admin/audit/filters", null, AdminPermissionPolicies.AuditView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Love Brackley audit", HttpMethod.Get, "/api/admin/love-brackley-audit", null, AdminPermissionPolicies.AuditView, HttpStatusCode.OK)];

        yield return [new RouteMatrixCase("Dashboard", HttpMethod.Get, "/api/admin/dashboard", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Businesses list", HttpMethod.Get, "/api/admin/businesses", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Businesses create", HttpMethod.Post, "/api/admin/businesses", new CreateAdminBusinessRequest(
            OwnerUserId: "owner-user-id",
            Name: "Green Pantry",
            BrandReference: "GP",
            Category: "Retail",
            Description: "Community shop",
            LocationName: "Green Pantry",
            AddressLine1: "1 Main Street",
            AddressLine2: null,
            Town: "Brackley",
            Postcode: "NN13 7AA",
            ContactEmail: "hello@example.com",
            SupportEmail: null,
            PrimaryContactName: "Owner Name",
            TradingHoursSummary: "9-5",
            Featured: true), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Business detail", HttpMethod.Get, "/api/admin/businesses/00000000-0000-0000-0000-000000000001", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Business update", HttpMethod.Put, "/api/admin/businesses/00000000-0000-0000-0000-000000000001", new AdminUpdateBusinessSettingsRequest(
            Name: "Green Pantry",
            Category: "Retail",
            Description: "Community shop",
            LocationName: "Green Pantry",
            AddressLine1: "1 Main Street",
            AddressLine2: null,
            Town: "Brackley",
            Postcode: "NN13 7AA",
            ContactEmail: "hello@example.com",
            SupportEmail: null,
            PrimaryContactName: "Owner Name",
            TradingHoursSummary: "9-5",
            Featured: true), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Business approve", HttpMethod.Post, "/api/admin/businesses/00000000-0000-0000-0000-000000000001/approve", null, AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];

        yield return [new RouteMatrixCase("Members list", HttpMethod.Get, "/api/admin/members", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Members create", HttpMethod.Post, "/api/admin/members", new CreateAdminMemberRequest(
            UserId: "user-1",
            DisplayName: "Test Member",
            Email: "member@example.com",
            MobileNumber: null,
            HomeArea: null), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Member detail", HttpMethod.Get, "/api/admin/members/00000000-0000-0000-0000-000000000002", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Member update", HttpMethod.Put, "/api/admin/members/00000000-0000-0000-0000-000000000002", new UpdateMemberSettingsRequest(
            DisplayName: "Test Member",
            MobileNumber: null,
            HomeArea: null,
            MarketingConsent: false,
            EmailUpdatesPreference: false,
            LocalOffersPreference: false,
            CardReminderPreference: false), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Member lock", HttpMethod.Post, "/api/admin/members/00000000-0000-0000-0000-000000000002/lock", null, AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Member adjust points", HttpMethod.Post, "/api/admin/members/00000000-0000-0000-0000-000000000002/adjust-points", new AdjustPointsRequest(Guid.Parse("00000000-0000-0000-0000-000000000003"), 10, "Test adjustment"), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];

        yield return [new RouteMatrixCase("Reviews list", HttpMethod.Get, "/api/admin/reviews", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Review detail", HttpMethod.Get, "/api/admin/reviews/00000000-0000-0000-0000-000000000004", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Review resolve", HttpMethod.Post, "/api/admin/reviews/00000000-0000-0000-0000-000000000004/resolve", new ResolveReviewCaseRequest("Resolved"), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Review escalate", HttpMethod.Post, "/api/admin/reviews/00000000-0000-0000-0000-000000000004/escalate", new EscalateReviewCaseRequest("Needs escalation"), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];

        yield return [new RouteMatrixCase("Transactions list", HttpMethod.Get, "/api/admin/transactions", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Access list", HttpMethod.Get, "/api/admin/access", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Access provisioning create", HttpMethod.Post, "/api/admin/access", new CreateBusinessStaffAccessProvisioningRequest(
            Guid.Parse("00000000-0000-0000-0000-000000000010"),
            "user-1",
            "Provisioned Staff",
            "Staff",
            "Suspended",
            "Create provisioning"), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Access provisioning update", HttpMethod.Put, "/api/admin/access/00000000-0000-0000-0000-000000000011/provisioning", new UpdateBusinessStaffAccessProvisioningRequest(
            "user-1",
            "Active",
            "Activate provisioning"), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Points rule read", HttpMethod.Get, "/api/admin/settings/points-rule", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Points rule update", HttpMethod.Put, "/api/admin/settings/points-rule", new UpdatePointsRuleRequest("Default", 10, 1, 10m), AdminPermissionPolicies.AdminConfigure, HttpStatusCode.OK)];
        yield return [new RouteMatrixCase("Payouts list", HttpMethod.Get, "/api/admin/payouts", null, AdminPermissionPolicies.AdminView, HttpStatusCode.OK)];
    }

    public static IEnumerable<object[]> DeniedCases()
    {
        foreach (var routeCase in AllowedCases().Select(data => (RouteMatrixCase)data[0]!))
        {
            yield return [routeCase];
        }
    }

    private static IEnumerable<RouteMatrixCase> GetRouteCases()
    {
        return AllowedCases().Select(data => (RouteMatrixCase)data[0]!);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, RouteMatrixCase routeCase)
    {
        using var request = new HttpRequestMessage(routeCase.Method, routeCase.Path);
        if (routeCase.Body is not null)
        {
            request.Content = JsonContent.Create(routeCase.Body);
        }

        return await client.SendAsync(request);
    }

    public sealed record RouteMatrixCase(
        string Name,
        HttpMethod Method,
        string Path,
        object? Body,
        string RequiredPermission,
        HttpStatusCode AllowedStatus);
}
