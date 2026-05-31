using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Identity;
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
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = "andy@example.com",
            UserName = "andy@example.com",
            DisplayName = "Andy Morgan",
            Status = "Active"
        };

        var createResult = await userManager.CreateAsync(user, "Password1!");

        Assert.True(createResult.Succeeded);

        var request = await repository.CreatePasswordResetRequestAsync(user.Email, CancellationToken.None);

        Assert.NotNull(request);

        var storedToken = await dbContext.PasswordResetTokens.SingleAsync();

        Assert.Equal(user.Id, storedToken.UserId);
        Assert.NotEqual(request!.Token, storedToken.TokenHash);
        Assert.Equal(64, storedToken.TokenHash.Length);
        Assert.Null(storedToken.UsedAtUtc);
        Assert.True(storedToken.ExpiresAtUtc > storedToken.CreatedAtUtc);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"password-reset-{Guid.NewGuid():N}")
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
}
