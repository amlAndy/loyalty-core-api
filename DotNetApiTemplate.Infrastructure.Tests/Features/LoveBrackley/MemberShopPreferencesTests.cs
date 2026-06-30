using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetApiTemplate.Infrastructure.Tests.Features.LoveBrackley;

public sealed class MemberShopPreferencesTests
{
    [Fact]
    public async Task SaveFavoriteAsync_creates_preference_when_business_and_member_exist()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);
        var request = new SaveMemberFavoriteRequest("Great local cafe");

        var result = await service.SaveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Coffee Shop", result.Name);
        Assert.Equal("Cafe", result.Category);
        Assert.True(result.ReceiveOfferNotifications);
        Assert.True(result.ReceiveUpdateNotifications);
        Assert.Equal("Great local cafe", result.Notes);
    }

    [Fact]
    public async Task SaveFavoriteAsync_returns_existing_preference_if_already_saved()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);
        var request = new SaveMemberFavoriteRequest("My favorite");

        var result1 = await service.SaveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), request, CancellationToken.None);
        var result2 = await service.SaveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), new SaveMemberFavoriteRequest("Updated notes"), CancellationToken.None);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.PreferenceId, result2.PreferenceId);

        var preferences = await dbContext.MemberShopPreferences
            .Where(p => p.MemberProfileId == Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") && p.RemovedUtc == null)
            .ToListAsync();
        Assert.Single(preferences);
    }

    [Fact]
    public async Task SaveFavoriteAsync_returns_null_when_business_not_found()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);
        var request = new SaveMemberFavoriteRequest("Notes");

        var result = await service.SaveFavoriteAsync("member-user-1", Guid.NewGuid(), request, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveFavoriteAsync_returns_null_when_member_not_found()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);
        var request = new SaveMemberFavoriteRequest("Notes");

        var result = await service.SaveFavoriteAsync("non-existent-user", Guid.Parse("11111111-1111-1111-1111-111111111111"), request, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_soft_deletes_preference()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);

        await service.SaveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), new SaveMemberFavoriteRequest(null), CancellationToken.None);
        var result = await service.RemoveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);

        Assert.True(result);

        var preference = await dbContext.MemberShopPreferences
            .FirstOrDefaultAsync(p => p.MemberProfileId == Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") && p.BusinessProfileId == Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.NotNull(preference);
        Assert.NotNull(preference.RemovedUtc);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_returns_false_when_preference_not_found()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.RemoveFavoriteAsync("member-user-1", Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_returns_false_when_member_not_found()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.RemoveFavoriteAsync("non-existent-user", Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task GetFavoritesAsync_returns_paginated_list_of_favorites()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);

        await service.SaveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), new SaveMemberFavoriteRequest(null), CancellationToken.None);
        await service.SaveFavoriteAsync("member-user-1", Guid.Parse("22222222-2222-2222-2222-222222222222"), new SaveMemberFavoriteRequest(null), CancellationToken.None);

        var result = await service.GetFavoritesAsync("member-user-1", 1, 20, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetFavoritesAsync_returns_null_when_member_not_found()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.GetFavoritesAsync("non-existent-user", 1, 20, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFavoritesAsync_excludes_removed_preferences()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);

        await service.SaveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), new SaveMemberFavoriteRequest(null), CancellationToken.None);
        await service.SaveFavoriteAsync("member-user-1", Guid.Parse("22222222-2222-2222-2222-222222222222"), new SaveMemberFavoriteRequest(null), CancellationToken.None);
        await service.RemoveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);

        var result = await service.GetFavoritesAsync("member-user-1", 1, 20, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Restaurant", result.Items[0].Name);
    }

    [Fact]
    public async Task UpdateFavoritePreferencesAsync_updates_notification_preferences()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);

        await service.SaveFavoriteAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), new SaveMemberFavoriteRequest(null), CancellationToken.None);
        var request = new UpdateMemberFavoritePreferencesRequest(false, true, "Updated notes");
        var result = await service.UpdateFavoritePreferencesAsync("member-user-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.ReceiveOfferNotifications);
        Assert.True(result.ReceiveUpdateNotifications);
        Assert.Equal("Updated notes", result.Notes);
    }

    [Fact]
    public async Task UpdateFavoritePreferencesAsync_returns_null_when_preference_not_found()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);
        var request = new UpdateMemberFavoritePreferencesRequest(false, true, null);

        var result = await service.UpdateFavoritePreferencesAsync("member-user-1", Guid.NewGuid(), request, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateFavoritePreferencesAsync_returns_null_when_member_not_found()
    {
        await using var dbContext = CreateDbContext();
        SeedTestData(dbContext);
        var service = new LoveBrackleyService(dbContext);
        var request = new UpdateMemberFavoritePreferencesRequest(false, true, null);

        var result = await service.UpdateFavoritePreferencesAsync("non-existent-user", Guid.Parse("11111111-1111-1111-1111-111111111111"), request, CancellationToken.None);

        Assert.Null(result);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SeedTestData(ApplicationDbContext dbContext)
    {
        var memberId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var businessId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var businessId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var member = new MemberProfileEntity
        {
            Id = memberId,
            UserId = "member-user-1",
            DisplayName = "Test Member",
            Email = "member@test.com",
            PublicMemberReference = "MEM001",
            CardCode = "CARD001",
            Status = "Active",
            PointsBalance = 1000,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        var business1 = new BusinessProfileEntity
        {
            Id = businessId1,
            OwnerUserId = "business-owner-1",
            Name = "Coffee Shop",
            BrandReference = "CS001",
            Category = "Cafe",
            Description = "Great local cafe",
            LocationName = "Main Street",
            AddressLine1 = "123 Main St",
            Town = "Brackley",
            Postcode = "NN13 7AZ",
            ContactEmail = "info@coffeeshop.com",
            PrimaryContactName = "John Doe",
            TradingHoursSummary = "9am-6pm",
            ParticipationStatus = "Active",
            Featured = true,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        var business2 = new BusinessProfileEntity
        {
            Id = businessId2,
            OwnerUserId = "business-owner-2",
            Name = "Restaurant",
            BrandReference = "REST001",
            Category = "Food",
            Description = "Fine dining restaurant",
            LocationName = "High Street",
            AddressLine1 = "456 High St",
            Town = "Brackley",
            Postcode = "NN13 7AB",
            ContactEmail = "info@restaurant.com",
            PrimaryContactName = "Jane Smith",
            TradingHoursSummary = "12pm-10pm",
            ParticipationStatus = "Active",
            Featured = false,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        dbContext.MemberProfiles.Add(member);
        dbContext.BusinessProfiles.AddRange(business1, business2);
        dbContext.SaveChanges();
    }
}
