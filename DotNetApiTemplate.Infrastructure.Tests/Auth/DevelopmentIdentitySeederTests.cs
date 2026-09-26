using System.Reflection;
using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetApiTemplate.Infrastructure.Tests.Auth;

public sealed class DevelopmentIdentitySeederTests
{
    [Fact]
    public async Task EnsureBusinessStaffAccessesAsync_ExistingMembersAndBusinesses_RestoresMissingSeededStaffAccesses()
    {
        await using var dbContext = CreateDbContext();
        SeedLoveBrackleyUsers(dbContext);
        SeedLoveBrackleyBusinesses(dbContext);
        SeedExistingMemberProfile(dbContext);

        var seeder = CreateSeeder(dbContext);
        var users = await LoadUsersByEmailAsync(dbContext);

        await InvokeEnsureBusinessStaffAccessesAsync(seeder, users);

        var seededStaff = await dbContext.BusinessStaffAccesses
            .Where(candidate =>
                candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444401") ||
                candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444402") ||
                candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444403") ||
                candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444404"))
            .ToListAsync();

        Assert.Equal(4, seededStaff.Count);

        Assert.Contains(seededStaff, candidate =>
            candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444401") &&
            candidate.BusinessProfileId == Guid.Parse("22222222-2222-2222-2222-222222222201") &&
            candidate.PinStatus == "Active");
    }

    [Fact]
    public async Task EnsureBusinessStaffAccessesAsync_Rerun_IsIdempotentAndDoesNotDuplicateSeededRows()
    {
        await using var dbContext = CreateDbContext();
        SeedLoveBrackleyUsers(dbContext);
        SeedLoveBrackleyBusinesses(dbContext);
        SeedExistingMemberProfile(dbContext);

        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
            BusinessProfileId = Guid.Parse("22222222-2222-2222-2222-222222222207"),
            UserId = "unrelated-user",
            DisplayName = "Unrelated Staff",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var seeder = CreateSeeder(dbContext);
        var users = await LoadUsersByEmailAsync(dbContext);

        await InvokeEnsureBusinessStaffAccessesAsync(seeder, users);
        await InvokeEnsureBusinessStaffAccessesAsync(seeder, users);

        var seededCount = await dbContext.BusinessStaffAccesses.CountAsync(candidate =>
            candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444401") ||
            candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444402") ||
            candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444403") ||
            candidate.Id == Guid.Parse("44444444-4444-4444-4444-444444444404"));

        Assert.Equal(4, seededCount);
        Assert.Equal(5, await dbContext.BusinessStaffAccesses.CountAsync());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static DevelopmentIdentitySeeder CreateSeeder(ApplicationDbContext dbContext)
    {
        var roleStore = new RoleStore<ApplicationRole, ApplicationDbContext, string>(dbContext);
        var roleManager = new RoleManager<ApplicationRole>(
            roleStore,
            [new RoleValidator<ApplicationRole>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<ApplicationRole>>.Instance);

        var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string>(dbContext);
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);

        return new DevelopmentIdentitySeeder(dbContext, roleManager, userManager);
    }

    private static async Task<Dictionary<string, string>> LoadUsersByEmailAsync(ApplicationDbContext dbContext)
    {
        return await dbContext.Users.ToDictionaryAsync(user => user.Email!, user => user.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task InvokeEnsureBusinessStaffAccessesAsync(DevelopmentIdentitySeeder seeder, IReadOnlyDictionary<string, string> users)
    {
        var method = typeof(DevelopmentIdentitySeeder).GetMethod("EnsureBusinessStaffAccessesAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var task = method!.Invoke(seeder, [users, CancellationToken.None]) as Task;
        Assert.NotNull(task);

        await task!;
    }

    private static void SeedLoveBrackleyUsers(ApplicationDbContext dbContext)
    {
        var emails = new[]
        {
            "owner.marketkitchen@love-brackley.local",
            "owner.oldtownbooks@love-brackley.local",
            "owner.florist@love-brackley.local",
            "owner.willowcoffee@love-brackley.local",
            "owner.homegift@love-brackley.local",
            "owner.foxpantry@love-brackley.local",
            "owner.riverside@love-brackley.local",
            "owner.greenpantry@love-brackley.local",
            "eleanor.price@love-brackley.local",
            "marcus.reed@love-brackley.local",
            "aisha.khan@love-brackley.local",
            "leo.martin@love-brackley.local",
            "member.seed@love-brackley.local",
        };

        foreach (var email in emails)
        {
            dbContext.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = email,
                UserName = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                DisplayName = email,
                Status = "Active"
            });
        }

        dbContext.SaveChanges();
    }

    private static void SeedLoveBrackleyBusinesses(ApplicationDbContext dbContext)
    {
        var users = dbContext.Users.ToDictionary(user => user.Email!, user => user.Id, StringComparer.OrdinalIgnoreCase);

        dbContext.BusinessProfiles.AddRange(
            CreateBusiness(Guid.Parse("22222222-2222-2222-2222-222222222201"), users["owner.marketkitchen@love-brackley.local"], "Brackley Market Kitchen", "LB-BIZ-2001"),
            CreateBusiness(Guid.Parse("22222222-2222-2222-2222-222222222202"), users["owner.oldtownbooks@love-brackley.local"], "Old Town Books", "LB-BIZ-2002"),
            CreateBusiness(Guid.Parse("22222222-2222-2222-2222-222222222203"), users["owner.florist@love-brackley.local"], "Love Brackley Florist", "LB-BIZ-2003"),
            CreateBusiness(Guid.Parse("22222222-2222-2222-2222-222222222204"), users["owner.willowcoffee@love-brackley.local"], "Willow Coffee House", "LB-BIZ-2004"),
            CreateBusiness(Guid.Parse("22222222-2222-2222-2222-222222222205"), users["owner.homegift@love-brackley.local"], "Brackley Home & Gift", "LB-BIZ-2005"),
            CreateBusiness(Guid.Parse("22222222-2222-2222-2222-222222222206"), users["owner.foxpantry@love-brackley.local"], "Fox Lane Pantry", "LB-BIZ-2006"),
            CreateBusiness(Guid.Parse("22222222-2222-2222-2222-222222222207"), users["owner.riverside@love-brackley.local"], "Riverside Market Kitchen", "LB-BIZ-2007"),
            CreateBusiness(Guid.Parse("22222222-2222-2222-2222-222222222208"), users["owner.greenpantry@love-brackley.local"], "Green Pantry", "LB-BIZ-2008"));

        dbContext.SaveChanges();
    }

    private static void SeedExistingMemberProfile(ApplicationDbContext dbContext)
    {
        var memberUserId = dbContext.Users.Single(user => user.Email == "member.seed@love-brackley.local").Id;

        dbContext.MemberProfiles.Add(new MemberProfileEntity
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111150"),
            UserId = memberUserId,
            DisplayName = "Seed Member",
            Email = "member.seed@love-brackley.local",
            MobileNumber = "07700900000",
            HomeArea = "Brackley",
            PublicMemberReference = "LBM-9150",
            CardCode = "LB-CARD-9150",
            Status = "Active",
            PointsBalance = 0,
            MarketingConsent = true,
            EmailUpdatesPreference = true,
            LocalOffersPreference = true,
            CardReminderPreference = true,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });

        dbContext.SaveChanges();
    }

    private static BusinessProfileEntity CreateBusiness(Guid id, string ownerUserId, string name, string brandReference)
    {
        return new BusinessProfileEntity
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Name = name,
            BrandReference = brandReference,
            Category = "Food & Drink",
            TownId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            BusinessCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Description = "Test business",
            LocationName = "Test location",
            AddressLine1 = "1 Test Street",
            Town = "Brackley",
            Postcode = "NN13 7AA",
            ContactEmail = "business@example.com",
            SupportEmail = "support@example.com",
            PrimaryContactName = name,
            TradingHoursSummary = "Mon-Fri 09:00-17:00",
            ParticipationStatus = "Active",
            Featured = true,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }
}
