using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.LoveBrackley;

public sealed class LoveBrackleyServiceBusinessMembersTests
{
    [Fact]
    public async Task SearchMembersAsync_NoActivityStatusFilter_UsesGlobalTotalAndPaging()
    {
        await using var dbContext = CreateDbContext();
        var businessId = SeedBusinessMembersScenario(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var page1 = await service.SearchMembersAsync(businessId, null, null, page: 1, pageSize: 3, CancellationToken.None);
        var page2 = await service.SearchMembersAsync(businessId, null, null, page: 2, pageSize: 3, CancellationToken.None);

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(8, page1!.TotalCount);
        Assert.Equal(8, page2!.TotalCount);
        Assert.Equal(3, page1.Items.Count);
        Assert.Equal(3, page2.Items.Count);
        Assert.Equal(new[] { "A Active Recent", "B Active Recent", "C Inactive One" }, page1.Items.Select(item => item.DisplayName).ToArray());
        Assert.Equal(new[] { "D Inactive Two", "E Inactive Three", "F Follow Up" }, page2.Items.Select(item => item.DisplayName).ToArray());
    }

    [Fact]
    public async Task SearchMembersAsync_ActivityStatusFilter_IsAppliedBeforePagingAndCount()
    {
        await using var dbContext = CreateDbContext();
        var businessId = SeedBusinessMembersScenario(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var page1 = await service.SearchMembersAsync(businessId, null, "Inactive", page: 1, pageSize: 2, CancellationToken.None);
        var page2 = await service.SearchMembersAsync(businessId, null, "Inactive", page: 2, pageSize: 2, CancellationToken.None);

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(3, page1!.TotalCount);
        Assert.Equal(3, page2!.TotalCount);
        Assert.Equal(new[] { "C Inactive One", "D Inactive Two" }, page1.Items.Select(item => item.DisplayName).ToArray());
        Assert.Equal(new[] { "E Inactive Three" }, page2.Items.Select(item => item.DisplayName).ToArray());
        Assert.All(page1.Items.Concat(page2.Items), item => Assert.Equal("Inactive", item.ActivityStatus));
    }

    [Fact]
    public async Task SearchMembersAsync_CombinedSearchAndStatus_ReturnsCorrectSliceAndTotal()
    {
        await using var dbContext = CreateDbContext();
        var businessId = SeedBusinessMembersScenario(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.SearchMembersAsync(businessId, "Inactive", "Inactive", page: 1, pageSize: 2, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(new[] { "C Inactive One", "D Inactive Two" }, result.Items.Select(item => item.DisplayName).ToArray());
    }

    [Fact]
    public async Task SearchMembersAsync_ZeroMatches_ReturnsEmptyItemsAndZeroTotal()
    {
        await using var dbContext = CreateDbContext();
        var businessId = SeedBusinessMembersScenario(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.SearchMembersAsync(businessId, "no-such-member", "Inactive", page: 1, pageSize: 20, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result!.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchMembersAsync_PageBeyondLastPage_ReturnsEmptyItemsWithStableTotal()
    {
        await using var dbContext = CreateDbContext();
        var businessId = SeedBusinessMembersScenario(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.SearchMembersAsync(businessId, null, "Inactive", page: 3, pageSize: 2, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.TotalCount);
        Assert.Empty(result.Items);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"love-brackley-members-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Guid SeedBusinessMembersScenario(ApplicationDbContext dbContext)
    {
        var businessId = Guid.Parse("22222222-2222-2222-2222-222222222290");
        var otherBusinessId = Guid.Parse("22222222-2222-2222-2222-222222222291");
        var now = DateTimeOffset.UtcNow;

        dbContext.BusinessProfiles.AddRange(
            CreateBusiness(businessId, "owner-1", "Business One"),
            CreateBusiness(otherBusinessId, "owner-2", "Business Two"));

        var activeRecentA = CreateMember(Guid.Parse("11111111-1111-1111-1111-111111111101"), "A Active Recent", "Active");
        var activeRecentB = CreateMember(Guid.Parse("11111111-1111-1111-1111-111111111102"), "B Active Recent", "Active");
        var inactiveOne = CreateMember(Guid.Parse("11111111-1111-1111-1111-111111111103"), "C Inactive One", "Active");
        var inactiveTwo = CreateMember(Guid.Parse("11111111-1111-1111-1111-111111111104"), "D Inactive Two", "Active");
        var inactiveThree = CreateMember(Guid.Parse("11111111-1111-1111-1111-111111111105"), "E Inactive Three", "Active");
        var followUp = CreateMember(Guid.Parse("11111111-1111-1111-1111-111111111106"), "F Follow Up", "Active");
        var suspended = CreateMember(Guid.Parse("11111111-1111-1111-1111-111111111107"), "G Suspended", "Suspended");
        var otherBusinessMember = CreateMember(Guid.Parse("11111111-1111-1111-1111-111111111108"), "Z Other Business Active", "Active");

        dbContext.MemberProfiles.AddRange(activeRecentA, activeRecentB, inactiveOne, inactiveTwo, inactiveThree, followUp, suspended, otherBusinessMember);

        dbContext.LoyaltyTransactions.AddRange(
            CreateTransaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), businessId, activeRecentA.Id, now.AddDays(-1)),
            CreateTransaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), businessId, activeRecentB.Id, now.AddDays(-2)),
            CreateTransaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), businessId, inactiveOne.Id, now.AddDays(-46)),
            CreateTransaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), businessId, inactiveTwo.Id, now.AddDays(-60)),
            CreateTransaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), businessId, inactiveThree.Id, now.AddDays(-90)),
            CreateTransaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"), businessId, suspended.Id, now.AddDays(-1)),
            CreateTransaction(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"), otherBusinessId, otherBusinessMember.Id, now.AddDays(-1)));

        dbContext.SaveChanges();
        return businessId;
    }

    private static BusinessProfileEntity CreateBusiness(Guid businessId, string ownerUserId, string name)
    {
        return new BusinessProfileEntity
        {
            Id = businessId,
            OwnerUserId = ownerUserId,
            Name = name,
            BrandReference = $"BR-{name}",
            Category = "Food & Drink",
            TownId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            BusinessCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Description = "Desc",
            LocationName = "Location",
            AddressLine1 = "1 Street",
            Town = "Brackley",
            Postcode = "NN13 7AA",
            ContactEmail = "business@example.com",
            PrimaryContactName = "Owner",
            TradingHoursSummary = "Mon-Fri",
            ParticipationStatus = "Active",
            Featured = false,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    private static MemberProfileEntity CreateMember(Guid id, string displayName, string status)
    {
        return new MemberProfileEntity
        {
            Id = id,
            UserId = $"user-{id:N}",
            DisplayName = displayName,
            Email = $"{displayName.Replace(" ", ".").ToLowerInvariant()}@example.com",
            MobileNumber = null,
            PublicMemberReference = $"REF-{displayName[0]}",
            CardCode = $"CARD-{displayName[0]}",
            Status = status,
            PointsBalance = 100,
            MarketingConsent = true,
            EmailUpdatesPreference = true,
            LocalOffersPreference = true,
            CardReminderPreference = true,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    private static LoyaltyTransactionEntity CreateTransaction(Guid id, Guid businessId, Guid memberId, DateTimeOffset createdUtc)
    {
        return new LoyaltyTransactionEntity
        {
            Id = id,
            BusinessProfileId = businessId,
            MemberProfileId = memberId,
            TransactionType = "Earn",
            Status = "Completed",
            Points = 10,
            PurchaseAmount = 10,
            RedeemedValue = 0,
            Reference = $"TX-{id:N}",
            CreatedUtc = createdUtc,
            CompletedUtc = createdUtc,
        };
    }
}
