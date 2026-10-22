using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Authorization;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.LoveBrackley;

public sealed class BusinessAccessAuthorizationServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_MemberRole_IsForbiddenForEarnAndRedeem()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAuthorizationScenarioAsync(dbContext);
        var service = new BusinessAccessAuthorizationService(dbContext);

        var earnResult = await service.AuthorizeAsync(
            seed.MemberUserId,
            seed.MemberRoleId,
            seed.ActiveBusinessId,
            BusinessAccessOperation.EarnPoints,
            CancellationToken.None);

        var redeemResult = await service.AuthorizeAsync(
            seed.MemberUserId,
            seed.MemberRoleId,
            seed.ActiveBusinessId,
            BusinessAccessOperation.RedeemPoints,
            CancellationToken.None);

        Assert.Equal(BusinessAccessDecision.Forbidden, earnResult.Decision);
        Assert.Equal(BusinessAccessDecision.Forbidden, redeemResult.Decision);
    }

    [Fact]
    public async Task AuthorizeAsync_BusinessOwner_InactiveParticipation_IsForbiddenForTransactions()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAuthorizationScenarioAsync(dbContext);
        var service = new BusinessAccessAuthorizationService(dbContext);

        var earnResult = await service.AuthorizeAsync(
            seed.OwnerUserId,
            seed.BusinessOwnerRoleId,
            seed.InactiveBusinessId,
            BusinessAccessOperation.EarnPoints,
            CancellationToken.None);

        var redeemResult = await service.AuthorizeAsync(
            seed.OwnerUserId,
            seed.BusinessOwnerRoleId,
            seed.InactiveBusinessId,
            BusinessAccessOperation.RedeemPoints,
            CancellationToken.None);

        Assert.Equal(BusinessAccessDecision.Forbidden, earnResult.Decision);
        Assert.Equal(BusinessAccessDecision.Forbidden, redeemResult.Decision);
    }

    [Fact]
    public async Task AuthorizeAsync_BusinessStaff_AssignedActiveAccess_IsAllowedForTransactions()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAuthorizationScenarioAsync(dbContext);
        var service = new BusinessAccessAuthorizationService(dbContext);

        var earnResult = await service.AuthorizeAsync(
            seed.StaffUserId,
            seed.BusinessStaffRoleId,
            seed.ActiveBusinessId,
            BusinessAccessOperation.EarnPoints,
            CancellationToken.None);

        var redeemResult = await service.AuthorizeAsync(
            seed.StaffUserId,
            seed.BusinessStaffRoleId,
            seed.ActiveBusinessId,
            BusinessAccessOperation.RedeemPoints,
            CancellationToken.None);

        Assert.Equal(BusinessAccessDecision.Allowed, earnResult.Decision);
        Assert.Equal(BusinessAccessDecision.Allowed, redeemResult.Decision);
    }

    [Fact]
    public async Task AuthorizeAsync_BusinessStaff_UnassignedBusiness_IsForbidden()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAuthorizationScenarioAsync(dbContext);
        var service = new BusinessAccessAuthorizationService(dbContext);

        var result = await service.AuthorizeAsync(
            seed.StaffUserId,
            seed.BusinessStaffRoleId,
            seed.OtherBusinessId,
            BusinessAccessOperation.RedeemPoints,
            CancellationToken.None);

        Assert.Equal(BusinessAccessDecision.Forbidden, result.Decision);
    }

    [Fact]
    public async Task AuthorizeAsync_BusinessStaff_UnassignedBusiness_IsForbiddenForMemberCardLookup()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAuthorizationScenarioAsync(dbContext);
        var service = new BusinessAccessAuthorizationService(dbContext);

        var result = await service.AuthorizeAsync(
            seed.StaffUserId,
            seed.BusinessStaffRoleId,
            seed.OtherBusinessId,
            BusinessAccessOperation.MemberCardLookup,
            CancellationToken.None);

        Assert.Equal(BusinessAccessDecision.Forbidden, result.Decision);
    }

    [Fact]
    public async Task AuthorizeAsync_BusinessOwner_OtherBusinessDashboard_IsForbidden()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAuthorizationScenarioAsync(dbContext);
        var service = new BusinessAccessAuthorizationService(dbContext);

        var result = await service.AuthorizeAsync(
            seed.OwnerUserId,
            seed.BusinessOwnerRoleId,
            seed.OtherBusinessId,
            BusinessAccessOperation.Dashboard,
            CancellationToken.None);

        Assert.Equal(BusinessAccessDecision.Forbidden, result.Decision);
    }

    [Fact]
    public async Task AuthorizeAsync_BusinessStaff_AmbiguousStaffRows_IsForbidden()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAuthorizationScenarioAsync(dbContext, includeDuplicateStaffRow: true);
        var service = new BusinessAccessAuthorizationService(dbContext);

        var result = await service.AuthorizeAsync(
            seed.StaffUserId,
            seed.BusinessStaffRoleId,
            seed.ActiveBusinessId,
            BusinessAccessOperation.RedeemPoints,
            CancellationToken.None);

        Assert.Equal(BusinessAccessDecision.Forbidden, result.Decision);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"business-access-authz-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<AuthzSeed> SeedAuthorizationScenarioAsync(ApplicationDbContext dbContext, bool includeDuplicateStaffRow = false)
    {
        var ownerRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessOwner);
        var staffRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessStaff);
        var memberRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.Member);

        var ownerUserId = "owner-user";
        var staffUserId = "staff-user";
        var memberUserId = "member-user";
        var otherOwnerUserId = "owner-other";

        dbContext.Users.AddRange(
            CreateUser(ownerUserId, "owner@test.local"),
            CreateUser(staffUserId, "staff@test.local"),
            CreateUser(memberUserId, "member@test.local"),
            CreateUser(otherOwnerUserId, "owner-other@test.local"));
        await dbContext.SaveChangesAsync();

        await AssignRoleAsync(dbContext, ownerUserId, ownerRoleId);
        await AssignRoleAsync(dbContext, staffUserId, staffRoleId);
        await AssignRoleAsync(dbContext, memberUserId, memberRoleId);

        var activeBusinessId = Guid.Parse("22222222-2222-2222-2222-222222222261");
        var inactiveBusinessId = Guid.Parse("22222222-2222-2222-2222-222222222262");
        var otherBusinessId = Guid.Parse("22222222-2222-2222-2222-222222222263");

        dbContext.BusinessProfiles.AddRange(
            CreateBusiness(activeBusinessId, ownerUserId, "Active"),
            CreateBusiness(inactiveBusinessId, ownerUserId, "Inactive"),
            CreateBusiness(otherBusinessId, otherOwnerUserId, "Active"));

        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444461"),
            BusinessProfileId = activeBusinessId,
            UserId = staffUserId,
            DisplayName = "Staff",
            BusinessAccessRole = "Staff",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });

        if (includeDuplicateStaffRow)
        {
            dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444462"),
                BusinessProfileId = activeBusinessId,
                UserId = staffUserId,
                DisplayName = "Staff Duplicate",
                BusinessAccessRole = "Staff",
                PinHash = "hash",
                PinStatus = "Active",
                LastActivitySummary = string.Empty,
                CreatedUtc = DateTimeOffset.UtcNow,
                UpdatedUtc = DateTimeOffset.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync();

        return new AuthzSeed(ownerUserId, staffUserId, memberUserId, ownerRoleId, staffRoleId, memberRoleId, activeBusinessId, inactiveBusinessId, otherBusinessId);
    }

    private static ApplicationUser CreateUser(string id, string email)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            DisplayName = email,
            Status = "Active",
        };
    }

    private static BusinessProfileEntity CreateBusiness(Guid id, string ownerUserId, string participationStatus)
    {
        return new BusinessProfileEntity
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Name = $"Business-{id:N}",
            BrandReference = $"BR-{id:N}",
            Category = "Food",
            TownId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            BusinessCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Description = "Desc",
            LocationName = "Location",
            AddressLine1 = "1 Street",
            Town = "Brackley",
            Postcode = "NN13 7AA",
            ContactEmail = "biz@test.local",
            PrimaryContactName = "Owner",
            TradingHoursSummary = "Mon-Fri",
            ParticipationStatus = participationStatus,
            Featured = false,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
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

    private sealed record AuthzSeed(
        string OwnerUserId,
        string StaffUserId,
        string MemberUserId,
        string BusinessOwnerRoleId,
        string BusinessStaffRoleId,
        string MemberRoleId,
        Guid ActiveBusinessId,
        Guid InactiveBusinessId,
        Guid OtherBusinessId);
}
