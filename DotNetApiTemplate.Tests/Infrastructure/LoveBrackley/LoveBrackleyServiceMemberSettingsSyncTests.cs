using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.LoveBrackley;

public sealed class LoveBrackleyServiceMemberSettingsSyncTests
{
    [Fact]
    public async Task UpdateSettingsAsync_UpdatesMemberAndLinkedUserDisplayNameTogether()
    {
        await using var dbContext = CreateDbContext();
        var userId = "member-user-1";
        var memberId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "member.user@example.com",
            Email = "member.user@example.com",
            DisplayName = "Original User Name",
            Status = "Active",
        });

        dbContext.MemberProfiles.Add(new MemberProfileEntity
        {
            Id = memberId,
            UserId = userId,
            DisplayName = "Original Profile Name",
            Email = "member.user@example.com",
            MobileNumber = "07700111222",
            HomeArea = "Brackley",
            PublicMemberReference = "MEM-AAA",
            CardCode = "CARD-AAA",
            Status = "Active",
            PointsBalance = 120,
            MarketingConsent = true,
            EmailUpdatesPreference = true,
            LocalOffersPreference = false,
            CardReminderPreference = true,
            CreatedUtc = DateTimeOffset.UtcNow.AddDays(-10),
            UpdatedUtc = DateTimeOffset.UtcNow.AddDays(-1),
        });
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var result = await service.UpdateSettingsAsync(
            userId,
            new UpdateMemberSettingsRequest(
                "  Updated Shared Name  ",
                "07700999888",
                "Brackley",
                true,
                false,
                true,
                true),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Shared Name", result!.DisplayName);

        var persistedMember = await dbContext.MemberProfiles.SingleAsync(candidate => candidate.Id == memberId);
        var persistedUser = await dbContext.Users.SingleAsync(candidate => candidate.Id == userId);

        Assert.Equal("Updated Shared Name", persistedMember.DisplayName);
        Assert.Equal("Updated Shared Name", persistedUser.DisplayName);
        Assert.Equal("Brackley", persistedMember.HomeArea);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"love-brackley-member-settings-sync-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
