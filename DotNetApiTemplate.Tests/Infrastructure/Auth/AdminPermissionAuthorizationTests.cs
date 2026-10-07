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

public sealed class AdminPermissionAuthorizationTests
{
    [Fact]
    public async Task AdminWithRequiredPermission_IsAuthorized()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context,
            (ApplicationRoleNames.PlatformAdmin, ["users.update"]),
            (ApplicationRoleNames.Member, []));
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "admin@example.com", "Admin User");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.PlatformAdmin], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAsync(scope.ServiceProvider, selected!.AccessToken, "users.update");
        Assert.True(authorized);
    }

    [Fact]
    public async Task AdminWithoutRequiredPermission_IsForbidden()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context,
            (ApplicationRoleNames.PlatformAdmin, []),
            (ApplicationRoleNames.Member, []));
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "admin-no-perm@example.com", "Admin No Perm");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.PlatformAdmin], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAsync(scope.ServiceProvider, selected!.AccessToken, "users.update");
        Assert.False(authorized);
    }

    [Fact]
    public async Task PermissionOnNonSelectedRole_IsForbidden()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context,
            (ApplicationRoleNames.PlatformAdmin, []),
            (ApplicationRoleNames.TownAdmin, ["users.update"]));
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "admin-non-selected@example.com", "Admin Non Selected");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.PlatformAdmin], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAsync(scope.ServiceProvider, selected!.AccessToken, "users.update");
        Assert.False(authorized);
    }

    [Fact]
    public async Task InactiveRolePermission_IsForbidden()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context,
            (ApplicationRoleNames.PlatformAdmin, []),
            (ApplicationRoleNames.TownAdmin, ["users.update"]));
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "admin-inactive@example.com", "Admin Inactive");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.PlatformAdmin], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAsync(scope.ServiceProvider, selected!.AccessToken, "users.update");
        Assert.False(authorized);
    }

    [Fact]
    public async Task NonAdminActiveRole_IsForbidden()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = CreateRepository(scope.ServiceProvider, context);

        var roles = await CreateRolesAsync(context,
            (ApplicationRoleNames.Member, ["users.update"]));
        var user = await CreateUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), "member@example.com", "Member User");
        await AssignRolesAsync(context, user.Id, roles.Values);

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        Assert.NotNull(session);

        var selected = await repository.SelectRoleAsync(session!.AccessToken, roles[ApplicationRoleNames.Member], CancellationToken.None);
        Assert.NotNull(selected);

        var authorized = await AuthorizeAsync(scope.ServiceProvider, selected!.AccessToken, "users.update");
        Assert.False(authorized);
    }

    [Fact]
    public async Task Unauthenticated_IsUnauthorized()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), resource: null, "users.update");

        Assert.False(result.Succeeded);
    }

    private static async Task<bool> AuthorizeAsync(IServiceProvider serviceProvider, string accessToken, string permissionCode)
    {
        var jwtTokenService = serviceProvider.GetRequiredService<JwtTokenService>();
        var principal = jwtTokenService.ValidateAccessToken(accessToken);
        Assert.NotNull(principal);

        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var result = await authorizationService.AuthorizeAsync(principal!, resource: null, permissionCode);
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
            options.UseInMemoryDatabase($"admin-permissions-{Guid.NewGuid():N}"));
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

            options.AddPolicy(
                "users.update",
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(ApplicationRoleNames.AdminRoles);
                    policy.AddRequirements(new ActiveRoleRequirement(ApplicationRoleNames.AdminRoles));
                    policy.AddRequirements(new ActiveRolePermissionRequirement("users.update"));
                });
        });

        services.AddScoped<IAuthorizationHandler, SelectedRoleAuthorizationHandler>();
        services.AddScoped<JwtTokenService>();

        return services.BuildServiceProvider();
    }

    private static async Task<Dictionary<string, string>> CreateRolesAsync(ApplicationDbContext context, params (string Name, string[] PermissionCodes)[] roles)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, permissionCodes) in roles)
        {
            var roleId = await CreateRoleAsync(context, name, permissionCodes);
            result[name] = roleId;
        }

        return result;
    }

    private static async Task<string> CreateRoleAsync(ApplicationDbContext context, string roleName, IEnumerable<string> permissionCodes)
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

        foreach (var permissionCode in permissionCodes.Distinct(StringComparer.Ordinal))
        {
            context.RoleClaims.Add(new IdentityRoleClaim<string>
            {
                RoleId = roleId,
                ClaimType = "permission",
                ClaimValue = permissionCode
            });
        }

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
