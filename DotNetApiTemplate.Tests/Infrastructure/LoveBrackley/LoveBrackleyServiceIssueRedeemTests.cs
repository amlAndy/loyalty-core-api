using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.LoveBrackley;

public sealed class LoveBrackleyServiceIssueRedeemTests
{
    [Fact]
    public async Task RedeemAsync_BusinessOwner_CompletesWithoutStaffAccessAttribution()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.RedeemAsync(
            seed.BusinessId,
            new RedeemPointsRequest(seed.MemberId, null, Points: 100, PurchaseAmount: 10m, Notes: "Owner redeem"),
            seed.OwnerUserId,
            seed.BusinessOwnerRoleId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Completed", result!.Status);
        Assert.Null(result.StaffAccessId);

        var member = await dbContext.MemberProfiles.SingleAsync(candidate => candidate.Id == seed.MemberId);
        Assert.Equal(900, member.PointsBalance);
    }

    [Fact]
    public async Task RedeemAsync_BusinessStaff_DerivesAuthenticatedStaffAccess()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.RedeemAsync(
            seed.BusinessId,
            new RedeemPointsRequest(seed.MemberId, null, Points: 100, PurchaseAmount: 10m, Notes: "Staff redeem"),
            seed.StaffUserId,
            seed.BusinessStaffRoleId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(seed.StaffAccessId, result!.StaffAccessId);
        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task RedeemAsync_BusinessStaff_DoesNotImpersonateAnotherStaffAccess()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScenarioAsync(dbContext);

        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444402"),
            BusinessProfileId = seed.BusinessId,
            UserId = seed.OtherStaffUserId,
            DisplayName = "Other Staff",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var result = await service.RedeemAsync(
            seed.BusinessId,
            new RedeemPointsRequest(seed.MemberId, null, Points: 100, PurchaseAmount: 10m, Notes: "Staff redeem"),
            seed.StaffUserId,
            seed.BusinessStaffRoleId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(seed.StaffAccessId, result!.StaffAccessId);
    }

    [Fact]
    public async Task RedeemAsync_BusinessStaff_WithoutActiveAccess_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScenarioAsync(dbContext, staffPinStatus: "ResetRequired");
        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RedeemAsync(
                seed.BusinessId,
                new RedeemPointsRequest(seed.MemberId, null, Points: 100, PurchaseAmount: 10m, Notes: null),
                seed.StaffUserId,
                seed.BusinessStaffRoleId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "staffAccess");
    }

    [Fact]
    public async Task RedeemAsync_BusinessStaff_WithAmbiguousAccess_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScenarioAsync(dbContext);

        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444499"),
            BusinessProfileId = seed.BusinessId,
            UserId = seed.StaffUserId,
            DisplayName = "Staff User Duplicate",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RedeemAsync(
                seed.BusinessId,
                new RedeemPointsRequest(seed.MemberId, null, Points: 100, PurchaseAmount: 10m, Notes: null),
                seed.StaffUserId,
                seed.BusinessStaffRoleId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "staffAccess" && error.ErrorMessage.Contains("Ambiguous"));
    }

    [Fact]
    public async Task EarnAsync_AppliesFloorRuleAndReturnsTransaction()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.EarnAsync(
            seed.BusinessId,
            new EarnPointsRequest(seed.MemberId, null, null, PurchaseAmount: 9.99m, Notes: "Earn"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Earn", result!.TransactionType);
        Assert.Equal(9, result.Points);

        var member = await dbContext.MemberProfiles.SingleAsync(candidate => candidate.Id == seed.MemberId);
        Assert.Equal(1009, member.PointsBalance);
    }

    [Fact]
    public async Task RedeemAsync_InsufficientPoints_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RedeemAsync(
                seed.BusinessId,
                new RedeemPointsRequest(seed.MemberId, null, Points: 5000, PurchaseAmount: 100000m, Notes: null),
                seed.OwnerUserId,
                seed.BusinessOwnerRoleId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "Points" && error.ErrorMessage.Contains("enough points"));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"love-brackley-issue-redeem-{Guid.NewGuid():N}")
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<TestSeedData> SeedScenarioAsync(ApplicationDbContext dbContext, string staffPinStatus = "Active")
    {
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var staffRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessStaff);
        await CreateRoleAsync(dbContext, ApplicationRoleNames.Member);

        var ownerUserId = "user-owner";
        var staffUserId = "user-staff";
        var otherStaffUserId = "user-staff-other";

        dbContext.Users.AddRange(
            CreateUser(ownerUserId, "owner@example.com", "Owner User"),
            CreateUser(staffUserId, "staff@example.com", "Staff User"),
            CreateUser(otherStaffUserId, "staff-other@example.com", "Staff Other User"));

        await dbContext.SaveChangesAsync();

        await AssignRoleAsync(dbContext, ownerUserId, ownerRoleId);
        await AssignRoleAsync(dbContext, staffUserId, staffRoleId);

        var businessId = Guid.Parse("22222222-2222-2222-2222-222222222251");
        var memberId = Guid.Parse("11111111-1111-1111-1111-111111111251");
        var staffAccessId = Guid.Parse("44444444-4444-4444-4444-444444444401");

        dbContext.BusinessProfiles.Add(CreateBusiness(businessId, ownerUserId, "Riverside Cafe", "Active"));
        dbContext.MemberProfiles.Add(CreateMember(memberId, "Olivia Carter", pointsBalance: 1000));
        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = staffAccessId,
            BusinessProfileId = businessId,
            UserId = staffUserId,
            DisplayName = "Staff User",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = staffPinStatus,
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });

        dbContext.LoyaltyPointsRules.Add(new LoyaltyPointsRuleEntity
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
            Name = "Default",
            EarnPointsPerPound = 1,
            PencePerPoint = 1,
            MaxRedemptionPercentage = 20,
            IsActive = true,
            EffectiveFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync();

        return new TestSeedData(businessId, memberId, ownerUserId, staffUserId, otherStaffUserId, ownerRoleId, staffRoleId, staffAccessId);
    }

    private static ApplicationUser CreateUser(string id, string email, string displayName)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            DisplayName = displayName,
            Status = "Active",
        };
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

    private static BusinessProfileEntity CreateBusiness(Guid businessId, string ownerUserId, string name, string participationStatus)
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
            ParticipationStatus = participationStatus,
            Featured = false,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    private static MemberProfileEntity CreateMember(Guid id, string displayName, int pointsBalance)
    {
        return new MemberProfileEntity
        {
            Id = id,
            UserId = $"member-user-{id:N}",
            DisplayName = displayName,
            Email = $"{displayName.Replace(" ", ".").ToLowerInvariant()}@example.com",
            MobileNumber = null,
            PublicMemberReference = "LBM-TEST",
            CardCode = "LB-CARD-TEST",
            Status = "Active",
            PointsBalance = pointsBalance,
            MarketingConsent = true,
            EmailUpdatesPreference = true,
            LocalOffersPreference = true,
            CardReminderPreference = true,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    private sealed record TestSeedData(
        Guid BusinessId,
        Guid MemberId,
        string OwnerUserId,
        string StaffUserId,
        string OtherStaffUserId,
        string BusinessOwnerRoleId,
        string BusinessStaffRoleId,
        Guid StaffAccessId);
}
