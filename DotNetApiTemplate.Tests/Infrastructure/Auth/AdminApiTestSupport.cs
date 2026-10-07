using System.Net.Http.Headers;
using System.Security.Claims;
using DotNetApiTemplate.Api.Controllers;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetApiTemplate.Tests.Infrastructure.Auth;

internal sealed record RoleSeed(string Name, string[] Permissions);

internal sealed class AdminApiFactory : WebApplicationFactory<AdminUsersController>
{
    private readonly string _databaseName = $"admin-api-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LoyaltyCoreDb"] = "Server=(localdb)\\MSSQLLocalDB;Database=AdminApiTests;Trusted_Connection=True;TrustServerCertificate=True;",
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:Secret"] = "12345678901234567890123456789012"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IAdminUserService>();
            services.RemoveAll<IAdminRoleService>();
            services.RemoveAll<IAuditService>();
            services.RemoveAll<IAdminLoveBrackleyService>();

            services.AddSingleton<IAdminUserService, FakeAdminUserService>();
            services.AddSingleton<IAdminRoleService, FakeAdminRoleService>();
            services.AddSingleton<IAuditService, FakeAuditService>();
            services.AddSingleton<IAdminLoveBrackleyService, FakeAdminLoveBrackleyService>();
            services.AddAuthentication(TestAuthenticationHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.Scheme, _ => { });
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string activeRoleName, params RoleSeed[] roleSeeds)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = Guid.NewGuid().ToString("N");
        var roleIds = new Dictionary<string, string>(StringComparer.Ordinal);
        var rootRoleId = Guid.NewGuid().ToString("N");

        foreach (var roleSeed in roleSeeds.DistinctBy(seed => seed.Name, StringComparer.Ordinal))
        {
            var roleId = Guid.NewGuid().ToString("N");
            roleIds[roleSeed.Name] = roleId;

            context.Roles.Add(new ApplicationRole
            {
                Id = roleId,
                Name = roleSeed.Name,
                NormalizedName = roleSeed.Name.ToUpperInvariant(),
                Description = roleSeed.Name,
                IsRootRole = false,
                IsSystemRole = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });

            context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = userId,
                RoleId = roleId
            });

            foreach (var permissionCode in roleSeed.Permissions.Distinct(StringComparer.Ordinal))
            {
                context.RoleClaims.Add(new IdentityRoleClaim<string>
                {
                    RoleId = roleId,
                    ClaimType = "permission",
                    ClaimValue = permissionCode
                });
            }
        }

        await context.SaveChangesAsync();

        if (!roleIds.TryGetValue(activeRoleName, out var activeRoleId))
        {
            throw new InvalidOperationException($"Active role '{activeRoleName}' was not seeded.");
        }

        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserIdHeaderName);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.ActiveRoleHeaderName);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.RolesHeaderName);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, userId);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.ActiveRoleHeaderName, activeRoleId);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeaderName, string.Join(',', roleSeeds.Select(seed => seed.Name)));
        return client;
    }
}

internal sealed class FakeAdminUserService : IAdminUserService
{
    public Task<AdminUserDetailModel> CreateAsync(AdminUserUpsertModel model, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AdminUserDetailModel
        {
            Id = "user-1",
            DisplayName = model.DisplayName,
            Email = model.Email,
            Status = model.Status,
            RoleIds = model.RoleIds,
            Roles = model.RoleIds.Select(roleId => $"Role {roleId}").ToArray()
        });
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<AdminUserFormOptionsModel> GetFormOptionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AdminUserFormOptionsModel
        {
            Statuses = ["Active", "Inactive"],
            Roles =
            [
                new AdminRoleOptionModel { Id = "role-1", Name = "PlatformAdmin", IsRootRole = true, IsSystemRole = true }
            ]
        });
    }

    public Task<AdminUserDetailModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var status = id.Contains("inactive", StringComparison.OrdinalIgnoreCase)
            ? "Inactive"
            : id.Contains("locked", StringComparison.OrdinalIgnoreCase)
                ? "Locked"
                : id.Contains("pending", StringComparison.OrdinalIgnoreCase)
                    ? "PendingInvite"
                    : "Active";

        return Task.FromResult<AdminUserDetailModel?>(new AdminUserDetailModel
        {
            Id = id,
            DisplayName = "Test User",
            Email = "test.user@example.com",
            Status = status,
            RoleIds = ["role-1"],
            Roles = ["PlatformAdmin"]
        });
    }

    public Task<AdminUserSearchResult> SearchUsersAsync(AdminUserSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AdminUserSearchResult
        {
            Items =
            [
                new AdminUserSummaryModel
                {
                    Id = "user-1",
                    DisplayName = "Test User",
                    Email = "test.user@example.com",
                    Status = "Active",
                    RoleIds = ["role-1"],
                    Roles = ["PlatformAdmin"]
                }
            ],
            Total = 1,
            PageNumber = criteria.PageNumber.GetValueOrDefault(1),
            PageSize = criteria.PageSize.GetValueOrDefault(10)
        });
    }

    public Task<AdminUserDetailModel?> UpdateAsync(string id, AdminUserUpsertModel model, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AdminUserDetailModel?>(new AdminUserDetailModel
        {
            Id = id,
            DisplayName = model.DisplayName,
            Email = model.Email,
            Status = model.Status,
            RoleIds = model.RoleIds,
            Roles = model.RoleIds.Select(roleId => $"Role {roleId}").ToArray()
        });
    }
}

internal sealed class FakeAdminRoleService : IAdminRoleService
{
    public Task<AdminRoleSearchResult> SearchAsync(AdminRoleSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AdminRoleSearchResult
        {
            Items = [CreateSummary("role-1", "PlatformAdmin", ["users.view", "roles.view"])],
            Total = 1,
            PageNumber = criteria.PageNumber.GetValueOrDefault(1),
            PageSize = criteria.PageSize.GetValueOrDefault(10)
        });
    }

    public Task<AdminRoleDetailModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AdminRoleDetailModel?>(new AdminRoleDetailModel
        {
            Id = id,
            Name = "PlatformAdmin",
            Description = "Platform admin role",
            ParentRoleId = null,
            IsSystemRole = true,
            IsRootRole = true,
            Permissions = ["users.view", "roles.view"],
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
            ParentRoleName = null,
            ChildCount = 0,
            IsProtected = true,
            ChildRoles = []
        });
    }

    public Task<AdminRoleDetailModel> CreateAsync(AdminRoleUpsertModel model, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AdminRoleDetailModel
        {
            Id = "role-created",
            Name = model.Name,
            Description = model.Description,
            ParentRoleId = model.ParentRoleId,
            IsSystemRole = false,
            IsRootRole = false,
            Permissions = model.Permissions,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
            ParentRoleName = null,
            ChildCount = 0,
            IsProtected = false,
            ChildRoles = []
        });
    }

    public Task<AdminRoleDetailModel?> UpdateAsync(string id, AdminRoleUpsertModel model, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AdminRoleDetailModel?>(new AdminRoleDetailModel
        {
            Id = id,
            Name = model.Name,
            Description = model.Description,
            ParentRoleId = model.ParentRoleId,
            IsSystemRole = false,
            IsRootRole = false,
            Permissions = model.Permissions,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
            ParentRoleName = null,
            ChildCount = 0,
            IsProtected = false,
            ChildRoles = []
        });
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<AdminRoleDeleteEligibilityModel?> GetDeleteEligibilityAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AdminRoleDeleteEligibilityModel?>(new AdminRoleDeleteEligibilityModel
        {
            CanDelete = true,
            Reason = null
        });
    }

    public Task<AdminRoleParentOptionModel[]> GetAvailableParentRolesAsync(string? currentRoleId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[]
        {
            new AdminRoleParentOptionModel { Id = "role-parent", Name = "Parent Role", ParentRoleId = null, IsRootRole = false, IsSystemRole = true }
        });
    }

    public Task<string[]> GetAvailablePermissionsAsync(string? parentRoleId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { "users.view", "users.update" });
    }

    public Task<string[]> ValidatePermissionsAsync(ValidateRolePermissionsModel model, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(model.Permissions);
    }

    public Task<AdminRoleSummaryModel[]> ListChildRolesAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { CreateSummary("role-child", "Child Role", ["users.view"]) });
    }

    public Task<AdminRoleFormOptionsModel> GetFormOptionsAsync(string? currentRoleId, string? selectedParentRoleId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AdminRoleFormOptionsModel
        {
            AvailableParentRoles = [new AdminRoleParentOptionModel { Id = "role-parent", Name = "Parent Role", ParentRoleId = null, IsRootRole = false, IsSystemRole = true }],
            Permissions = [CreatePermission("users.view"), CreatePermission("users.update")],
            AvailablePermissions = [CreatePermission("users.view")],
            ProtectedRootRoleId = "role-root"
        });
    }

    private static AdminRoleSummaryModel CreateSummary(string id, string name, string[] permissions)
    {
        return new AdminRoleSummaryModel
        {
            Id = id,
            Name = name,
            Description = name,
            ParentRoleId = null,
            IsSystemRole = true,
            IsRootRole = false,
            Permissions = permissions,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
            ParentRoleName = null,
            ChildCount = 0,
            IsProtected = false
        };
    }

    private static AdminPermissionModel CreatePermission(string code)
    {
        return new AdminPermissionModel { Code = code, Description = code };
    }
}

internal sealed class FakeAuditService : IAuditService
{
    public Task<AuditSearchResult> SearchAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuditSearchResult
        {
            Items =
            [
                new AuditEntryModel
                {
                    Id = "audit-1",
                    OccurredAt = DateTimeOffset.UtcNow.ToString("O"),
                    ActingUser = "Test Admin",
                    Action = "UserCreated",
                    ActionLabel = "User Created",
                    EntityType = "User",
                    TargetId = "user-1",
                    TargetName = "Test User",
                    Target = "Test User",
                    Summary = "Created test user.",
                    Details = []
                }
            ],
            Total = 1,
            PageNumber = criteria.PageNumber.GetValueOrDefault(1),
            PageSize = criteria.PageSize.GetValueOrDefault(10)
        });
    }

    public Task<AuditFilterOptionsModel> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuditFilterOptionsModel
        {
            Actions = [new AuditFilterOptionModel { Value = "UserCreated", Label = "User Created" }],
            EntityTypes = [new AuditFilterOptionModel { Value = "User", Label = "User" }],
            ActingUsers = [new AuditFilterOptionModel { Value = "user-1", Label = "Test Admin" }]
        });
    }
}

internal sealed class FakeAdminLoveBrackleyService : IAdminLoveBrackleyService
{
    public Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new AdminDashboardDto(
            [new BusinessDashboardMetricDto("Businesses", "1", "info")],
            [new AdminQueueItemDto("Queue", 1, "info")],
            ["Review"],
            ["Notice"]));
    }

    public Task<PagedResponse<AdminBusinessDto>> SearchBusinessesAsync(string? searchTerm, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(Page(CreateBusiness(), page, pageSize));
    }

    public Task<AdminBusinessDto?> CreateBusinessAsync(CreateAdminBusinessRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult<AdminBusinessDto?>(CreateBusiness(request.Name));
    }

    public Task<BusinessSettingsDto?> GetBusinessAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return Task.FromResult<BusinessSettingsDto?>(CreateBusinessSettings(businessId));
    }

    public Task<BusinessSettingsDto?> UpdateBusinessAsync(Guid businessId, UpdateBusinessSettingsRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult<BusinessSettingsDto?>(CreateBusinessSettings(businessId, request.Name));
    }

    public Task<AdminBusinessDto?> SetBusinessStatusAsync(Guid businessId, string status, string action, string? actorUserId, CancellationToken cancellationToken)
    {
        var business = CreateBusiness();
        return Task.FromResult<AdminBusinessDto?>(business with { Id = businessId, ParticipationStatus = status });
    }

    public Task<PagedResponse<AdminMemberDto>> SearchMembersAsync(string? searchTerm, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(Page(CreateMember(), page, pageSize));
    }

    public Task<AdminMemberDto?> CreateMemberAsync(CreateAdminMemberRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<AdminMemberDto?>(CreateMember(request.UserId, request.DisplayName, request.Email));
    }

    public Task<AdminMemberDetailDto?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken)
    {
        return Task.FromResult<AdminMemberDetailDto?>(CreateMemberDetail(memberId));
    }

    public Task<AdminMemberDetailDto?> UpdateMemberAsync(Guid memberId, UpdateMemberSettingsRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<AdminMemberDetailDto?>(CreateMemberDetail(memberId, request.DisplayName));
    }

    public Task<AdminMemberDto?> SetMemberStatusAsync(Guid memberId, string status, CancellationToken cancellationToken)
    {
        return Task.FromResult<AdminMemberDto?>(CreateMember(memberId: memberId, status: status));
    }

    public Task<LoyaltyTransactionDto?> AdjustPointsAsync(Guid memberId, AdjustPointsRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult<LoyaltyTransactionDto?>(CreateTransaction(memberId, request.BusinessId, request.Points));
    }

    public Task<PagedResponse<AdminReviewCaseDto>> SearchReviewsAsync(string? caseType, string? status, string? priority, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(Page(CreateReviewCase(), page, pageSize));
    }

    public Task<AdminReviewCaseDto?> GetReviewAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return Task.FromResult<AdminReviewCaseDto?>(CreateReviewCase(caseId));
    }

    public Task<AdminReviewCaseDto?> ResolveReviewAsync(Guid caseId, ResolveReviewCaseRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult<AdminReviewCaseDto?>(CreateReviewCase(caseId, status: "Resolved"));
    }

    public Task<AdminReviewCaseDto?> EscalateReviewAsync(Guid caseId, EscalateReviewCaseRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult<AdminReviewCaseDto?>(CreateReviewCase(caseId, status: "Escalated"));
    }

    public Task<PagedResponse<LoyaltyTransactionDto>> SearchTransactionsAsync(Guid? businessId, Guid? memberId, string? transactionType, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(Page(CreateTransaction(memberId ?? Guid.NewGuid(), businessId ?? Guid.NewGuid(), 10), page, pageSize));
    }

    public Task<PagedResponse<BusinessStaffAccessRecordDto>> SearchAccessAsync(Guid? businessId, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(Page(CreateAccessRecord(businessId ?? Guid.NewGuid()), page, pageSize));
    }

    public Task<PagedResponse<BusinessPayoutDto>> SearchPayoutsAsync(Guid? businessId, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(Page(CreatePayout(businessId ?? Guid.NewGuid()), page, pageSize));
    }

    public Task<PointsRuleDto?> GetPointsRuleAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<PointsRuleDto?>(CreatePointsRule());
    }

    public Task<PointsRuleDto> UpdatePointsRuleAsync(UpdatePointsRuleRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(CreatePointsRule(request.Name));
    }

    public Task<PagedResponse<AdminAuditLogDto>> SearchAuditAsync(string? entityType, string? entityId, string? actorUserId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(Page(CreateAdminAuditLog(), page, pageSize));
    }

    private static PagedResponse<T> Page<T>(T item, int page, int pageSize)
    {
        return new PagedResponse<T>([item], page, pageSize, 1);
    }

    private static AdminDashboardDto CreateDashboard()
    {
        return new AdminDashboardDto(
            [new BusinessDashboardMetricDto("Businesses", "1", "info")],
            [new AdminQueueItemDto("Queue", 1, "info")],
            ["Review"],
            ["Notice"]);
    }

    private static AdminBusinessDto CreateBusiness(string? name = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new AdminBusinessDto(Guid.NewGuid(), name ?? "Green Pantry", "GP", "Retail", Guid.NewGuid(), "Retail", "retail", Guid.NewGuid(), "Brackley", "brackley", "Active", true, Guid.NewGuid().ToString("N"), now, now);
    }

    private static BusinessSettingsDto CreateBusinessSettings(Guid businessId, string? name = null)
    {
        return new BusinessSettingsDto(
            BusinessId: businessId,
            Name: name ?? "Green Pantry",
            Category: "Retail",
            BusinessCategoryId: Guid.NewGuid(),
            CategoryName: "Retail",
            CategorySlug: "retail",
            Description: "Business",
            LocationName: "1 Main Street",
            AddressLine1: string.Empty,
            AddressLine2: "Brackley",
            Town: "Brackley",
            TownId: Guid.NewGuid(),
            TownName: "Brackley",
            TownSlug: "brackley",
            Postcode: "NN13 7AA",
            ContactEmail: "hello@example.com",
            SupportEmail: null,
            PrimaryContactName: "Owner",
            TradingHoursSummary: "9-5",
            ParticipationStatus: "Active",
            Featured: true);
    }

    private static AdminMemberDto CreateMember(string? userId = null, string? displayName = null, string? email = null, string? status = "Active", Guid? memberId = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new AdminMemberDto(memberId ?? Guid.NewGuid(), userId ?? Guid.NewGuid().ToString("N"), "MBR-001", displayName ?? "Test Member", email ?? "member@example.com", status ?? "Active", 100, now, now);
    }

    private static AdminMemberDetailDto CreateMemberDetail(Guid memberId, string? displayName = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new AdminMemberDetailDto(memberId, Guid.NewGuid().ToString("N"), "MBR-001", displayName ?? "Test Member", "member@example.com", "07123456789", "Central", true, true, false, true, "Active", 100, now, now);
    }

    private static LoyaltyTransactionDto CreateTransaction(Guid memberId, Guid businessId, int points)
    {
        var now = DateTimeOffset.UtcNow;
        return new LoyaltyTransactionDto(Guid.NewGuid(), memberId, "Test Member", businessId, "Green Pantry", null, null, "Adjust", points, 10m, 0m, "Completed", "REF-1", null, now, now, null);
    }

    private static BusinessStaffAccessRecordDto CreateAccessRecord(Guid businessId)
    {
        var now = DateTimeOffset.UtcNow;
        return new BusinessStaffAccessRecordDto(Guid.NewGuid(), businessId, Guid.NewGuid().ToString("N"), "Test Staff", "Manager", "Active", "Logged in", now, now, now);
    }

    private static BusinessPayoutDto CreatePayout(Guid businessId)
    {
        var now = DateTimeOffset.UtcNow;
        return new BusinessPayoutDto(Guid.NewGuid(), businessId, now.AddDays(-7), now, 100m, 5m, 95m, "Pending", now, null, null);
    }

    private static PointsRuleDto CreatePointsRule(string? name = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new PointsRuleDto(Guid.NewGuid(), name ?? "Default", 10, 1, 10m, true, now.AddDays(-1), null, Guid.NewGuid().ToString("N"), now);
    }

    private static AdminReviewCaseDto CreateReviewCase(Guid? caseId = null, string? status = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new AdminReviewCaseDto(caseId ?? Guid.NewGuid(), "Issue", status ?? "Open", "Normal", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Test Case", "Test Description", null, null, now, null);
    }

    private static AdminAuditLogDto CreateAdminAuditLog()
    {
        var now = DateTimeOffset.UtcNow;
        return new AdminAuditLogDto(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "User", Guid.NewGuid().ToString("N"), "UserCreated", "Created test user", null, null, null, now);
    }
}
