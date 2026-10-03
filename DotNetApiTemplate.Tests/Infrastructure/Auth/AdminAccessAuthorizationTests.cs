using System.Security.Claims;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.Auth;

public sealed class AdminAccessAuthorizationTests
{
    [Fact]
    public async Task PlatformAdmin_WithActivePlatformAdminRole_IsAuthorized()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context, ApplicationRoleNames.PlatformAdmin, ApplicationRoleNames.Member);
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "admin-platform@example.com", "Platform Admin");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.PlatformAdmin], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAdminAccessAsync(scope.ServiceProvider, selected!.AccessToken);
        Assert.True(authorized);
    }

    [Fact]
    public async Task TownAdmin_WithActiveTownAdminRole_IsAuthorized()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context, ApplicationRoleNames.TownAdmin, ApplicationRoleNames.Member);
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "admin-town@example.com", "Town Admin");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.TownAdmin], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAdminAccessAsync(scope.ServiceProvider, selected!.AccessToken);
        Assert.True(authorized);
    }

    [Fact]
    public async Task PlatformAdminAndMember_WithActiveMemberRole_IsNotAuthorized()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context, ApplicationRoleNames.PlatformAdmin, ApplicationRoleNames.Member);
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "admin-member@example.com", "Admin Member");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.Member], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAdminAccessAsync(scope.ServiceProvider, selected!.AccessToken);
        Assert.False(authorized);
    }

    [Fact]
    public async Task PlatformAdminAndBusinessOwner_WithActiveBusinessOwnerRole_IsNotAuthorized()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context, ApplicationRoleNames.PlatformAdmin, ApplicationRoleNames.BusinessOwner);
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "admin-owner@example.com", "Admin Owner");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.BusinessOwner], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAdminAccessAsync(scope.ServiceProvider, selected!.AccessToken);
        Assert.False(authorized);
    }

    [Fact]
    public async Task RoleSwitch_FromAdminToNonAdmin_Denies_AfterSwitchingBackToAdmin_Allows()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context, ApplicationRoleNames.PlatformAdmin, ApplicationRoleNames.Member);
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "switch-user@example.com", "Switch User");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var adminSession = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.PlatformAdmin], CancellationToken.None);
        Assert.NotNull(adminSession);
        Assert.True(await AuthorizeAdminAccessAsync(scope.ServiceProvider, adminSession!.AccessToken));

        var memberSession = await repository.SelectRoleAsync(adminSession.AccessToken, roles[ApplicationRoleNames.Member], CancellationToken.None);
        Assert.NotNull(memberSession);
        Assert.False(await AuthorizeAdminAccessAsync(scope.ServiceProvider, memberSession!.AccessToken));

        var adminSessionAgain = await repository.SelectRoleAsync(memberSession.AccessToken, roles[ApplicationRoleNames.PlatformAdmin], CancellationToken.None);
        Assert.NotNull(adminSessionAgain);
        Assert.True(await AuthorizeAdminAccessAsync(scope.ServiceProvider, adminSessionAgain!.AccessToken));
    }

    [Fact]
    public async Task NonAdminUser_IsNotAuthorized()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context, ApplicationRoleNames.Member);
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "member-only@example.com", "Member Only");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var authorized = await AuthorizeAdminAccessAsync(scope.ServiceProvider, session!.AccessToken);
        Assert.False(authorized);
    }

    [Fact]
    public async Task MalformedActiveRoleId_IsNotAuthorized()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var roleId = await CreateRoleAsync(context, ApplicationRoleNames.PlatformAdmin);
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "malformed-active@example.com", "Malformed Active");
        await AssignRolesAsync(context, user.Id, [roleId]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, ApplicationRoleNames.PlatformAdmin),
            new Claim(JwtTokenService.ActiveRoleIdClaimType, "not-a-real-role-id")
        ], "Tests"));

        var authorized = await AuthorizeAdminAccessAsync(scope.ServiceProvider, principal);
        Assert.False(authorized);
    }

    [Fact]
    public async Task Refresh_PreservesSelectedRole_ForAdminAndNonAdminAuthorizationBehavior()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context, ApplicationRoleNames.PlatformAdmin, ApplicationRoleNames.Member);
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "refresh-switch@example.com", "Refresh Switch");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var adminSession = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.PlatformAdmin], CancellationToken.None);
        Assert.NotNull(adminSession);

        var adminRefreshed = await repository.RefreshAsync(adminSession!.RefreshToken, CancellationToken.None);
        Assert.NotNull(adminRefreshed);
        Assert.True(await AuthorizeAdminAccessAsync(scope.ServiceProvider, adminRefreshed!.AccessToken));

        var memberSession = await repository.SelectRoleAsync(adminRefreshed.AccessToken, roles[ApplicationRoleNames.Member], CancellationToken.None);
        Assert.NotNull(memberSession);

        var memberRefreshed = await repository.RefreshAsync(memberSession!.RefreshToken, CancellationToken.None);
        Assert.NotNull(memberRefreshed);
        Assert.False(await AuthorizeAdminAccessAsync(scope.ServiceProvider, memberRefreshed!.AccessToken));
    }

    private static async Task<bool> AuthorizeAdminAccessAsync(IServiceProvider serviceProvider, string accessToken)
    {
        var jwtTokenService = serviceProvider.GetRequiredService<JwtTokenService>();
        var principal = jwtTokenService.ValidateAccessToken(accessToken);
        Assert.NotNull(principal);

        return await AuthorizeAdminAccessAsync(serviceProvider, principal!);
    }

    private static async Task<bool> AuthorizeAdminAccessAsync(IServiceProvider serviceProvider, ClaimsPrincipal principal)
    {
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var result = await authorizationService.AuthorizeAsync(principal, resource: null, AdminAuthorizationPolicies.AdminAccess);
        return result.Succeeded;
    }

    private static IdentityUserAuthRepository CreateRepository(IServiceProvider serviceProvider, ApplicationDbContext context)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var jwtTokenService = serviceProvider.GetRequiredService<JwtTokenService>();
        return new IdentityUserAuthRepository(context, userManager, jwtTokenService);
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:Secret"] = "12345678901234567890123456789012"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"admin-access-{Guid.NewGuid():N}"));
        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AdminAuthorizationPolicies.AdminAccess,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(ApplicationRoleNames.AdminRoles);
                    policy.AddRequirements(new ActiveRoleRequirement(ApplicationRoleNames.AdminRoles));
                });
        });

        services.AddScoped<IAuthorizationHandler, SelectedRoleAuthorizationHandler>();
        services.AddScoped<JwtTokenService>();

        return services.BuildServiceProvider();
    }

    private static async Task<Dictionary<string, string>> CreateRolesAsync(ApplicationDbContext context, params string[] roleNames)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var roleName in roleNames.Distinct(StringComparer.Ordinal))
        {
            var roleId = await CreateRoleAsync(context, roleName);
            result[roleName] = roleId;
        }

        return result;
    }

    private static async Task<string> CreateRoleAsync(ApplicationDbContext context, string roleName)
    {
        var roleId = Guid.NewGuid().ToString("N");
        context.Roles.Add(new ApplicationRole
        {
            Id = roleId,
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            Description = roleName,
            IsRootRole = false,
            IsSystemRole = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        await context.SaveChangesAsync();
        return roleId;
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string displayName)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = email,
            UserName = email,
            DisplayName = displayName,
            Status = "Active"
        };

        var createResult = await userManager.CreateAsync(user, "Password1!");
        Assert.True(createResult.Succeeded);

        return user;
    }

    private static async Task AssignRolesAsync(ApplicationDbContext context, string userId, IEnumerable<string> roleIds)
    {
        context.UserRoles.AddRange(roleIds.Distinct(StringComparer.Ordinal).Select(roleId => new IdentityUserRole<string>
        {
            UserId = userId,
            RoleId = roleId
        }));

        await context.SaveChangesAsync();
    }
}
