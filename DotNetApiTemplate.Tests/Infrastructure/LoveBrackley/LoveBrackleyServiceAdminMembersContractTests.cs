using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.LoveBrackley;

public sealed class LoveBrackleyServiceAdminMembersContractTests
{
    [Fact]
    public async Task GetMemberAsync_Returns_AllEditableFields_WithPersistedValues()
    {
        await using var dbContext = CreateDbContext();
        var memberId = Guid.Parse("11111111-1111-1111-1111-111111111401");
        var createdUtc = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedUtc = DateTimeOffset.UtcNow.AddDays(-1);
        dbContext.MemberProfiles.Add(CreateMember(
            memberId,
            userId: "admin-member-user-1",
            displayName: "Member One",
            email: "member.one@example.com",
            mobileNumber: "07700111222",
            homeArea: "Brackley",
            marketingConsent: true,
            emailUpdatesPreference: false,
            localOffersPreference: true,
            cardReminderPreference: false,
            status: "Active",
            pointsBalance: 245,
            createdUtc: createdUtc,
            updatedUtc: updatedUtc));
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var result = await service.GetMemberAsync(memberId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(memberId, result!.Id);
        Assert.Equal("admin-member-user-1", result.UserId);
        Assert.Equal("MEM-111", result.PublicMemberReference);
        Assert.Equal("Member One", result.DisplayName);
        Assert.Equal("member.one@example.com", result.Email);
        Assert.Equal("07700111222", result.MobileNumber);
        Assert.Equal("Brackley", result.HomeArea);
        Assert.True(result.MarketingConsent);
        Assert.False(result.EmailUpdatesPreference);
        Assert.True(result.LocalOffersPreference);
        Assert.False(result.CardReminderPreference);
        Assert.Equal("Active", result.Status);
        Assert.Equal(245, result.PointsBalance);
        Assert.Equal(createdUtc, result.CreatedUtc);
        Assert.Equal(updatedUtc, result.UpdatedUtc);
    }

    [Fact]
    public async Task GetMemberAsync_ReturnsNullOptionalValues_AsNull()
    {
        await using var dbContext = CreateDbContext();
        var memberId = Guid.Parse("11111111-1111-1111-1111-111111111402");
        dbContext.MemberProfiles.Add(CreateMember(
            memberId,
            userId: "admin-member-user-2",
            displayName: "Member Two",
            email: "member.two@example.com",
            mobileNumber: null,
            homeArea: null,
            marketingConsent: false,
            emailUpdatesPreference: false,
            localOffersPreference: false,
            cardReminderPreference: false,
            status: "Active",
            pointsBalance: 100,
            createdUtc: DateTimeOffset.UtcNow.AddDays(-3),
            updatedUtc: DateTimeOffset.UtcNow.AddDays(-2)));
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var result = await service.GetMemberAsync(memberId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.MobileNumber);
        Assert.Null(result.HomeArea);
    }

    [Fact]
    public async Task DetailToUpdate_RoundTripWithNoChanges_PreservesEditableValues()
    {
        await using var dbContext = CreateDbContext();
        var memberId = Guid.Parse("11111111-1111-1111-1111-111111111403");
        dbContext.MemberProfiles.Add(CreateMember(
            memberId,
            userId: "admin-member-user-3",
            displayName: "Member Three",
            email: "member.three@example.com",
            mobileNumber: "07700999000",
            homeArea: "Towcester",
            marketingConsent: true,
            emailUpdatesPreference: true,
            localOffersPreference: false,
            cardReminderPreference: true,
            status: "Active",
            pointsBalance: 90,
            createdUtc: DateTimeOffset.UtcNow.AddDays(-4),
            updatedUtc: DateTimeOffset.UtcNow.AddDays(-2)));
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);
        var before = await service.GetMemberAsync(memberId, CancellationToken.None);
        Assert.NotNull(before);

        var request = new UpdateMemberSettingsRequest(
            before!.DisplayName,
            before.MobileNumber,
            before.HomeArea,
            before.MarketingConsent,
            before.EmailUpdatesPreference,
            before.LocalOffersPreference,
            before.CardReminderPreference);

        var updateResult = await service.UpdateMemberAsync(memberId, request, CancellationToken.None);
        var after = await service.GetMemberAsync(memberId, CancellationToken.None);

        Assert.NotNull(updateResult);
        Assert.NotNull(after);
        Assert.Equal(before.DisplayName, after!.DisplayName);
        Assert.Equal(before.MobileNumber, after.MobileNumber);
        Assert.Equal(before.HomeArea, after.HomeArea);
        Assert.Equal(before.MarketingConsent, after.MarketingConsent);
        Assert.Equal(before.EmailUpdatesPreference, after.EmailUpdatesPreference);
        Assert.Equal(before.LocalOffersPreference, after.LocalOffersPreference);
        Assert.Equal(before.CardReminderPreference, after.CardReminderPreference);
    }

    [Fact]
    public async Task UpdateMemberAsync_ChangingOneEditableField_DoesNotAlterOtherEditableOrReadOnlyFields()
    {
        await using var dbContext = CreateDbContext();
        var memberId = Guid.Parse("11111111-1111-1111-1111-111111111404");
        var createdUtc = DateTimeOffset.UtcNow.AddDays(-12);
        dbContext.MemberProfiles.Add(CreateMember(
            memberId,
            userId: "admin-member-user-4",
            displayName: "Member Four",
            email: "member.four@example.com",
            mobileNumber: "07700444000",
            homeArea: "Banbury",
            marketingConsent: false,
            emailUpdatesPreference: true,
            localOffersPreference: true,
            cardReminderPreference: false,
            status: "Suspended",
            pointsBalance: 33,
            createdUtc: createdUtc,
            updatedUtc: DateTimeOffset.UtcNow.AddDays(-5)));
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);
        var before = await service.GetMemberAsync(memberId, CancellationToken.None);
        Assert.NotNull(before);

        var request = new UpdateMemberSettingsRequest(
            "Member Four Updated",
            before!.MobileNumber,
            before.HomeArea,
            before.MarketingConsent,
            before.EmailUpdatesPreference,
            before.LocalOffersPreference,
            before.CardReminderPreference);

        var result = await service.UpdateMemberAsync(memberId, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Member Four Updated", result!.DisplayName);

        Assert.Equal(before.MobileNumber, result.MobileNumber);
        Assert.Equal(before.HomeArea, result.HomeArea);
        Assert.Equal(before.MarketingConsent, result.MarketingConsent);
        Assert.Equal(before.EmailUpdatesPreference, result.EmailUpdatesPreference);
        Assert.Equal(before.LocalOffersPreference, result.LocalOffersPreference);
        Assert.Equal(before.CardReminderPreference, result.CardReminderPreference);

        Assert.Equal(before.Id, result.Id);
        Assert.Equal(before.UserId, result.UserId);
        Assert.Equal(before.PublicMemberReference, result.PublicMemberReference);
        Assert.Equal(before.Email, result.Email);
        Assert.Equal(before.Status, result.Status);
        Assert.Equal(before.PointsBalance, result.PointsBalance);
        Assert.Equal(createdUtc, result.CreatedUtc);
    }

    [Fact]
    public async Task SearchMembersAsync_ContractRemainsSummaryShape()
    {
        await using var dbContext = CreateDbContext();
        dbContext.MemberProfiles.Add(CreateMember(
            Guid.Parse("11111111-1111-1111-1111-111111111405"),
            userId: "admin-member-user-5",
            displayName: "Member Five",
            email: "member.five@example.com",
            mobileNumber: "07700555000",
            homeArea: "Brackley",
            marketingConsent: true,
            emailUpdatesPreference: true,
            localOffersPreference: true,
            cardReminderPreference: true,
            status: "Active",
            pointsBalance: 10,
            createdUtc: DateTimeOffset.UtcNow.AddDays(-8),
            updatedUtc: DateTimeOffset.UtcNow.AddDays(-1)));
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var result = await service.SearchMembersAsync(null, null, 1, 20, CancellationToken.None);

        Assert.Single(result.Items);
        var member = result.Items[0];
        var propertyNames = typeof(AdminMemberDto).GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain("MobileNumber", propertyNames);
        Assert.DoesNotContain("HomeArea", propertyNames);
        Assert.DoesNotContain("MarketingConsent", propertyNames);
        Assert.DoesNotContain("EmailUpdatesPreference", propertyNames);
        Assert.DoesNotContain("LocalOffersPreference", propertyNames);
        Assert.DoesNotContain("CardReminderPreference", propertyNames);
        Assert.Equal("Member Five", member.DisplayName);
    }

    [Fact]
    public async Task GetMemberAndUpdateMember_MissingMember_ReturnNull()
    {
        await using var dbContext = CreateDbContext();
        var service = new LoveBrackleyService(dbContext);
        var missingId = Guid.Parse("11111111-1111-1111-1111-111111111499");

        var getResult = await service.GetMemberAsync(missingId, CancellationToken.None);
        var updateResult = await service.UpdateMemberAsync(
            missingId,
            new UpdateMemberSettingsRequest("Missing Member", null, null, false, false, false, false),
            CancellationToken.None);

        Assert.Null(getResult);
        Assert.Null(updateResult);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"love-brackley-admin-members-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static MemberProfileEntity CreateMember(
        Guid id,
        string userId,
        string displayName,
        string? email,
        string? mobileNumber,
        string? homeArea,
        bool marketingConsent,
        bool emailUpdatesPreference,
        bool localOffersPreference,
        bool cardReminderPreference,
        string status,
        int pointsBalance,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        return new MemberProfileEntity
        {
            Id = id,
            UserId = userId,
            DisplayName = displayName,
            Email = email,
            MobileNumber = mobileNumber,
            HomeArea = homeArea,
            PublicMemberReference = $"MEM-{id.ToString("N")[..3]}",
            CardCode = $"CARD-{id.ToString("N")[..3]}",
            Status = status,
            PointsBalance = pointsBalance,
            MarketingConsent = marketingConsent,
            EmailUpdatesPreference = emailUpdatesPreference,
            LocalOffersPreference = localOffersPreference,
            CardReminderPreference = cardReminderPreference,
            CreatedUtc = createdUtc,
            UpdatedUtc = updatedUtc,
        };
    }
}
