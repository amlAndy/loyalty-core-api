using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.LoveBrackley;

public sealed class LoveBrackleyServiceBusinessDashboardIsolationTests
{
    [Fact]
    public async Task GetDashboardAsync_UsesOnlyRequestedBusinessCompletedTransactionsForToday()
    {
        await using var dbContext = CreateDbContext();
        var seed = SeedScenario(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.GetDashboardAsync(seed.GreenPantryBusinessId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(seed.GreenPantryBusinessId, result!.BusinessId);
        Assert.Equal("Green Pantry", result.BusinessName);

        Assert.Contains(result.Metrics, metric => metric.Label == "Points issued today" && metric.Value == "69");
        Assert.Contains(result.Metrics, metric => metric.Label == "Points redeemed today" && metric.Value == "30");
        Assert.Contains(result.Metrics, metric => metric.Label == "Transactions today" && metric.Value == "3");

        Assert.Equal(3, result.RecentTransactions.Count);
        Assert.All(result.RecentTransactions, transaction => Assert.Equal("Completed", transaction.Status));
        Assert.All(result.RecentTransactions, transaction => Assert.Equal(seed.GreenPantryBusinessId, GetBusinessIdByTransactionId(dbContext, transaction.Id)));

        var ordered = result.RecentTransactions.Select(transaction => transaction.CreatedUtc).ToArray();
        Assert.Equal(ordered.OrderByDescending(value => value).ToArray(), ordered);
    }

    private static Guid GetBusinessIdByTransactionId(ApplicationDbContext dbContext, Guid transactionId)
    {
        return dbContext.LoyaltyTransactions
            .AsNoTracking()
            .Where(transaction => transaction.Id == transactionId)
            .Select(transaction => transaction.BusinessProfileId)
            .Single();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"business-dashboard-isolation-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static DashboardSeed SeedScenario(ApplicationDbContext dbContext)
    {
        var greenPantryBusinessId = Guid.Parse("32222222-2222-2222-2222-222222222208");
        var otherBusinessId = Guid.Parse("32222222-2222-2222-2222-222222222299");

        dbContext.BusinessProfiles.AddRange(
            CreateBusiness(greenPantryBusinessId, "owner-green", "Green Pantry"),
            CreateBusiness(otherBusinessId, "owner-other", "Other Business"));

        var memberA = CreateMember(Guid.Parse("41111111-1111-1111-1111-111111111101"), "Member A");
        var memberB = CreateMember(Guid.Parse("41111111-1111-1111-1111-111111111102"), "Member B");

        dbContext.MemberProfiles.AddRange(memberA, memberB);

        var (todayStartUtc, tomorrowStartUtc) = GetLondonDayRangeUtc(DateTimeOffset.UtcNow);

        dbContext.LoyaltyTransactions.AddRange(
            CreateTransaction(Guid.Parse("51111111-1111-1111-1111-111111111101"), greenPantryBusinessId, memberA.Id, "Earn", 48, 48.75m, 0m, "Completed", todayStartUtc.AddHours(2), "gp-today-earn-1"),
            CreateTransaction(Guid.Parse("51111111-1111-1111-1111-111111111102"), greenPantryBusinessId, memberA.Id, "Redeem", 30, 32.40m, 0.30m, "Completed", todayStartUtc.AddHours(5), "gp-today-redeem-1"),
            CreateTransaction(Guid.Parse("51111111-1111-1111-1111-111111111103"), greenPantryBusinessId, memberB.Id, "Earn", 21, 21.30m, 0m, "Completed", todayStartUtc.AddHours(7), "gp-today-earn-2"),
            CreateTransaction(Guid.Parse("51111111-1111-1111-1111-111111111104"), greenPantryBusinessId, memberB.Id, "Earn", 15, 15.00m, 0m, "Completed", todayStartUtc.AddDays(-1).AddHours(10), "gp-older-earn"),
            CreateTransaction(Guid.Parse("51111111-1111-1111-1111-111111111105"), greenPantryBusinessId, memberA.Id, "Redeem", 15, 10.00m, 0.15m, "Held", todayStartUtc.AddHours(3), "gp-held"),
            CreateTransaction(Guid.Parse("51111111-1111-1111-1111-111111111106"), otherBusinessId, memberA.Id, "Earn", 999, 999.00m, 0m, "Completed", todayStartUtc.AddHours(4), "other-today-earn"),
            CreateTransaction(Guid.Parse("51111111-1111-1111-1111-111111111107"), otherBusinessId, memberB.Id, "Redeem", 250, 125.00m, 2.50m, "Completed", todayStartUtc.AddHours(6), "other-today-redeem"));

        dbContext.SaveChanges();

        return new DashboardSeed(greenPantryBusinessId, otherBusinessId, todayStartUtc, tomorrowStartUtc);
    }

    private static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetLondonDayRangeUtc(DateTimeOffset utcNow)
    {
        var londonTimeZone = ResolveLondonTimeZone();
        var londonNow = TimeZoneInfo.ConvertTime(utcNow, londonTimeZone);
        var londonDate = londonNow.Date;

        var startLocal = new DateTime(londonDate.Year, londonDate.Month, londonDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddDays(1);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, londonTimeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, londonTimeZone);

        return (new DateTimeOffset(startUtc), new DateTimeOffset(endUtc));
    }

    private static TimeZoneInfo ResolveLondonTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
    }

    private static BusinessProfileEntity CreateBusiness(Guid id, string ownerUserId, string name)
    {
        return new BusinessProfileEntity
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Name = name,
            BrandReference = $"BR-{id:N}",
            Category = "Groceries",
            TownId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            BusinessCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Description = "desc",
            LocationName = "loc",
            AddressLine1 = "1 Street",
            Town = "Brackley",
            Postcode = "NN13 7AA",
            ContactEmail = "business@test.local",
            PrimaryContactName = "Owner",
            TradingHoursSummary = "Mon-Sat",
            ParticipationStatus = "Active",
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    private static MemberProfileEntity CreateMember(Guid id, string name)
    {
        return new MemberProfileEntity
        {
            Id = id,
            UserId = $"user-{id:N}",
            PublicMemberReference = $"LBM-{id:N}".Substring(0, 16),
            DisplayName = name,
            CardCode = $"CARD-{id:N}".Substring(0, 14),
            Status = "Active",
            PointsBalance = 1000,
            HomeArea = "Brackley",
            MarketingConsent = true,
            EmailUpdatesPreference = true,
            LocalOffersPreference = true,
            CardReminderPreference = true,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    private static LoyaltyTransactionEntity CreateTransaction(
        Guid id,
        Guid businessId,
        Guid memberId,
        string type,
        int points,
        decimal purchaseAmount,
        decimal redeemedValue,
        string status,
        DateTimeOffset createdUtc,
        string notes)
    {
        return new LoyaltyTransactionEntity
        {
            Id = id,
            BusinessProfileId = businessId,
            MemberProfileId = memberId,
            TransactionType = type,
            Points = points,
            PurchaseAmount = purchaseAmount,
            RedeemedValue = redeemedValue,
            Status = status,
            Reference = $"REF-{id:N}".Substring(0, 18),
            Notes = notes,
            CreatedUtc = createdUtc,
            CompletedUtc = status == "Completed" ? createdUtc : null,
        };
    }

    private sealed record DashboardSeed(
        Guid GreenPantryBusinessId,
        Guid OtherBusinessId,
        DateTimeOffset TodayStartUtc,
        DateTimeOffset TomorrowStartUtc);
}
