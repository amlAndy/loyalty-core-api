using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.Auth;

public sealed class IdentityUserAuthRepositoryTests
{
    [Fact]
    public async Task CreatePasswordResetRequestAsync_PersistsHashedTokenInApplicationDbContext()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var user = await CreateUserAsync(userManager, "andy@example.com", "Andy Morgan");

        var request = await repository.CreatePasswordResetRequestAsync(user.Email!, CancellationToken.None);

        Assert.NotNull(request);

        var storedToken = await dbContext.PasswordResetTokens.SingleAsync();

        Assert.Equal(user.Id, storedToken.UserId);
        Assert.NotEqual(request!.Token, storedToken.TokenHash);
        Assert.Equal(64, storedToken.TokenHash.Length);
        Assert.Null(storedToken.UsedAtUtc);
        Assert.True(storedToken.ExpiresAtUtc > storedToken.CreatedAtUtc);
    }

    [Fact]
    public async Task AuthenticateAsync_MemberSession_HasNullBusinessId()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var memberRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.Member);
        var user = await CreateUserAsync(userManager, "member@example.com", "Member User");
        await AssignRoleAsync(dbContext, user.Id, memberRoleId);

        var result = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.User.BusinessId);
    }

    [Fact]
    public async Task AuthenticateAsync_BusinessOwnerSession_UsesOwnedBusinessProfileId()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var user = await CreateUserAsync(userManager, "owner@example.com", "Owner User");
        await AssignRoleAsync(dbContext, user.Id, ownerRoleId);

        var businessId = Guid.Parse("22222222-2222-2222-2222-222222222201");
        dbContext.BusinessProfiles.Add(CreateBusinessProfile(businessId, user.Id, "Owner business"));
        await dbContext.SaveChangesAsync();

        var result = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(businessId.ToString("D"), result!.User.BusinessId);
    }

    [Fact]
    public async Task AuthenticateAsync_BusinessStaffSession_UsesAssignedActiveStaffBusinessProfileId()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var staffRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessStaff);
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var owner = await CreateUserAsync(userManager, "owner-staff-test@example.com", "Owner Staff Test");
        var staffUser = await CreateUserAsync(userManager, "staff@example.com", "Staff User");
        await AssignRoleAsync(dbContext, owner.Id, ownerRoleId);
        await AssignRoleAsync(dbContext, staffUser.Id, staffRoleId);

        var businessId = Guid.Parse("22222222-2222-2222-2222-222222222202");
        dbContext.BusinessProfiles.Add(CreateBusinessProfile(businessId, owner.Id, "Staff business"));
        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
            BusinessProfileId = businessId,
            UserId = staffUser.Id,
            DisplayName = "Staff User",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var result = await repository.AuthenticateAsync(staffUser.Email!, "Password1!", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(businessId.ToString("D"), result!.User.BusinessId);
    }

    [Fact]
    public async Task AuthenticateAsync_BusinessStaffSession_PreservesBusinessIdAcrossMeAndRefresh()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var staffRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessStaff);
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var owner = await CreateUserAsync(userManager, "owner-staff-flow@example.com", "Owner Staff Flow");
        var staffUser = await CreateUserAsync(userManager, "staff-flow@example.com", "Staff Flow");
        await AssignRoleAsync(dbContext, owner.Id, ownerRoleId);
        await AssignRoleAsync(dbContext, staffUser.Id, staffRoleId);

        var businessId = Guid.Parse("22222222-2222-2222-2222-222222222212");
        dbContext.BusinessProfiles.Add(CreateBusinessProfile(businessId, owner.Id, "Staff flow business"));
        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444412"),
            BusinessProfileId = businessId,
            UserId = staffUser.Id,
            DisplayName = "Staff Flow",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var session = await repository.AuthenticateAsync(staffUser.Email!, "Password1!", CancellationToken.None);
        var currentUser = await repository.GetUserByAccessTokenAsync(session!.AccessToken, CancellationToken.None);
        var refreshed = await repository.RefreshAsync(session.RefreshToken, CancellationToken.None);

        Assert.Equal(businessId.ToString("D"), session.User.BusinessId);
        Assert.Equal(businessId.ToString("D"), currentUser!.BusinessId);
        Assert.Equal(businessId.ToString("D"), refreshed!.User.BusinessId);
    }

    [Fact]
    public async Task AuthenticateAsync_BusinessStaffSession_WithAmbiguousActiveBusinesses_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var staffRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessStaff);
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var owner = await CreateUserAsync(userManager, "owner-staff-ambiguity@example.com", "Owner Staff Ambiguity");
        var staffUser = await CreateUserAsync(userManager, "staff-ambiguity@example.com", "Staff Ambiguity");
        await AssignRoleAsync(dbContext, owner.Id, ownerRoleId);
        await AssignRoleAsync(dbContext, staffUser.Id, staffRoleId);

        var businessA = Guid.Parse("22222222-2222-2222-2222-222222222213");
        var businessB = Guid.Parse("22222222-2222-2222-2222-222222222214");
        dbContext.BusinessProfiles.Add(CreateBusinessProfile(businessA, owner.Id, "Ambiguous A"));
        dbContext.BusinessProfiles.Add(CreateBusinessProfile(businessB, owner.Id, "Ambiguous B"));
        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444413"),
            BusinessProfileId = businessA,
            UserId = staffUser.Id,
            DisplayName = "Staff Ambiguity",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444414"),
            BusinessProfileId = businessB,
            UserId = staffUser.Id,
            DisplayName = "Staff Ambiguity",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AuthenticateAsync(staffUser.Email!, "Password1!", CancellationToken.None));
    }

    [Fact]
    public async Task AuthenticateAsync_AdminSession_HasNullBusinessId()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var adminRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.PlatformAdmin);
        var user = await CreateUserAsync(userManager, "admin@example.com", "Admin User");
        await AssignRoleAsync(dbContext, user.Id, adminRoleId);

        var result = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.User.BusinessId);
    }

    [Fact]
    public async Task RefreshAsync_PreservesBusinessIdForBusinessOwner()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var user = await CreateUserAsync(userManager, "owner-refresh@example.com", "Owner Refresh");
        await AssignRoleAsync(dbContext, user.Id, ownerRoleId);

        var businessId = Guid.Parse("22222222-2222-2222-2222-222222222203");
        dbContext.BusinessProfiles.Add(CreateBusinessProfile(businessId, user.Id, "Owner refresh business"));
        await dbContext.SaveChangesAsync();

        var login = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        var refreshed = await repository.RefreshAsync(login!.RefreshToken, CancellationToken.None);

        Assert.NotNull(refreshed);
        Assert.Equal(businessId.ToString("D"), refreshed!.User.BusinessId);
    }

    [Fact]
    public async Task SelectRoleAsync_UpdatesBusinessIdWhenSwitchingBetweenMemberAndBusinessOwner()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());

        var memberRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.Member);
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var user = await CreateUserAsync(userManager, "owner-member@example.com", "Owner Member");
        await AssignRoleAsync(dbContext, user.Id, memberRoleId);
        await AssignRoleAsync(dbContext, user.Id, ownerRoleId);

        var businessId = Guid.Parse("22222222-2222-2222-2222-222222222204");
        dbContext.BusinessProfiles.Add(CreateBusinessProfile(businessId, user.Id, "Owner member business"));
        await dbContext.SaveChangesAsync();

        var session = await repository.CreateSessionAsync(user.Email!, CancellationToken.None);
        Assert.NotNull(session);
        Assert.Null(session!.User.BusinessId);

        var ownerSession = await repository.SelectRoleAsync(session.AccessToken, ownerRoleId, CancellationToken.None);
        Assert.NotNull(ownerSession);
        Assert.Equal(businessId.ToString("D"), ownerSession!.User.BusinessId);

        var memberSession = await repository.SelectRoleAsync(ownerSession.AccessToken, memberRoleId, CancellationToken.None);
        Assert.NotNull(memberSession);
        Assert.Null(memberSession!.User.BusinessId);
    }

    [Fact]
    public async Task GetUserByAccessTokenAsync_ReturnsBusinessIdForActiveBusinessOwnerRole()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var repository = new IdentityUserAuthRepository(dbContext, userManager, CreateJwtTokenService());
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var user = await CreateUserAsync(userManager, "owner-me@example.com", "Owner Me");
        await AssignRoleAsync(dbContext, user.Id, ownerRoleId);

        var businessId = Guid.Parse("22222222-2222-2222-2222-222222222205");
        dbContext.BusinessProfiles.Add(CreateBusinessProfile(businessId, user.Id, "Owner me business"));
        await dbContext.SaveChangesAsync();

        var session = await repository.AuthenticateAsync(user.Email!, "Password1!", CancellationToken.None);
        var currentUser = await repository.GetUserByAccessTokenAsync(session!.AccessToken, CancellationToken.None);

        Assert.NotNull(currentUser);
        Assert.Equal(businessId.ToString("D"), currentUser!.BusinessId);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"auth-session-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext dbContext)
    {
        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string>(dbContext);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var lookupNormalizer = new UpperInvariantLookupNormalizer();
        var errorDescriber = new IdentityErrorDescriber();
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();

        return new UserManager<ApplicationUser>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            lookupNormalizer,
            errorDescriber,
            services,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private static JwtTokenService CreateJwtTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:Secret"] = "12345678901234567890123456789012"
            })
            .Build();

        return new JwtTokenService(configuration);
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

    private static async Task<string> CreateRoleAsync(ApplicationDbContext dbContext, string roleName)
    {
        var roleId = Guid.NewGuid().ToString("N");
        dbContext.Roles.Add(new ApplicationRole
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

        await dbContext.SaveChangesAsync();
        return roleId;
    }

    private static async Task AssignRoleAsync(ApplicationDbContext dbContext, string userId, string roleId)
    {
        dbContext.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = userId,
            RoleId = roleId,
        });

        await dbContext.SaveChangesAsync();
    }

    private static BusinessProfileEntity CreateBusinessProfile(Guid businessId, string ownerUserId, string name)
    {
        return new BusinessProfileEntity
        {
            Id = businessId,
            OwnerUserId = ownerUserId,
            Name = name,
            BrandReference = "LB-BIZ-TEST",
            Category = "Food & Drink",
            TownId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            BusinessCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Description = "Test business",
            LocationName = "Test location",
            AddressLine1 = "1 Test Street",
            Town = "Brackley",
            Postcode = "NN13 7AA",
            ContactEmail = "business@example.com",
            PrimaryContactName = "Test Owner",
            TradingHoursSummary = "Mon-Fri 09:00-17:00",
            ParticipationStatus = "Active",
            Featured = false,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }
}
