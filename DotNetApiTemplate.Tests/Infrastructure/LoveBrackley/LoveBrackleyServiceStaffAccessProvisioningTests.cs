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

public sealed class LoveBrackleyServiceStaffAccessProvisioningTests
{
    [Fact]
    public async Task CreateAccessProvisioningAsync_UnlinkedSuspended_CreatesRecord_AndWritesAuditWithoutCredentialMaterial()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProvisioningScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var result = await service.CreateAccessProvisioningAsync(
            new CreateBusinessStaffAccessProvisioningRequest(
                seed.BusinessA,
                null,
                "Unlinked Staff",
                "Staff",
                "Suspended",
                "Created for onboarding"),
            actorUserId: seed.AdminUserId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Suspended", result!.AccessStatus);
        Assert.Null(result.UserId);
        Assert.Equal("ResetRequired", result.PinStatus);

        var audit = await dbContext.AdminAuditLogs.OrderByDescending(entry => entry.CreatedUtc).FirstAsync();
        Assert.Equal("BusinessStaffAccess", audit.EntityType);
        Assert.Equal("CreateProvisioning", audit.Action);
        Assert.DoesNotContain("PinHash", audit.AfterJson ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1234", audit.AfterJson ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", audit.AfterJson ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAccessProvisioningAsync_LinkActive_UpdatesRecord_AndWritesAuditWithoutCredentialMaterial()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProvisioningScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var created = await service.CreateAccessProvisioningAsync(
            new CreateBusinessStaffAccessProvisioningRequest(
                seed.BusinessA,
                null,
                "Provision Target",
                "Staff",
                "Suspended",
                "Pending link"),
            actorUserId: seed.AdminUserId,
            CancellationToken.None);

        var updated = await service.UpdateAccessProvisioningAsync(
            created!.Id,
            new UpdateBusinessStaffAccessProvisioningRequest(seed.StaffActiveUserId, "Active", "Linked and activated"),
            actorUserId: seed.AdminUserId,
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(seed.StaffActiveUserId, updated!.UserId);
        Assert.Equal("Active", updated.AccessStatus);

        var updateAudit = await dbContext.AdminAuditLogs
            .Where(entry => entry.Action == "UpdateProvisioning")
            .OrderByDescending(entry => entry.CreatedUtc)
            .FirstAsync();

        Assert.NotNull(updateAudit.BeforeJson);
        Assert.NotNull(updateAudit.AfterJson);
        Assert.DoesNotContain("PinHash", updateAudit.BeforeJson!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinHash", updateAudit.AfterJson!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1234", updateAudit.BeforeJson!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1234", updateAudit.AfterJson!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAccessProvisioningAsync_ActiveWithoutLinkedUser_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProvisioningScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAccessProvisioningAsync(
                new CreateBusinessStaffAccessProvisioningRequest(seed.BusinessA, null, "Invalid Active", "Staff", "Active", null),
                seed.AdminUserId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "userId");
    }

    [Fact]
    public async Task CreateAccessProvisioningAsync_InactiveUser_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProvisioningScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAccessProvisioningAsync(
                new CreateBusinessStaffAccessProvisioningRequest(seed.BusinessA, seed.StaffInactiveUserId, "Inactive User", "Staff", "Active", null),
                seed.AdminUserId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "userId");
    }

    [Fact]
    public async Task CreateAccessProvisioningAsync_UserWithoutBusinessStaffRole_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProvisioningScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAccessProvisioningAsync(
                new CreateBusinessStaffAccessProvisioningRequest(seed.BusinessA, seed.StaffNoRoleUserId, "No Role", "Staff", "Active", null),
                seed.AdminUserId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "userId");
    }

    [Fact]
    public async Task CreateAccessProvisioningAsync_InvalidStatus_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProvisioningScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAccessProvisioningAsync(
                new CreateBusinessStaffAccessProvisioningRequest(seed.BusinessA, seed.StaffActiveUserId, "Invalid Status", "Staff", "Pending", null),
                seed.AdminUserId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "accessStatus");
    }

    [Fact]
    public async Task CreateAccessProvisioningAsync_ConflictingActiveAssignmentAcrossBusinesses_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProvisioningScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.NewGuid(),
            BusinessProfileId = seed.BusinessA,
            UserId = seed.StaffActiveUserId,
            DisplayName = "Existing Active",
            BusinessAccessRole = "Staff",
            AccessStatus = "Active",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAccessProvisioningAsync(
                new CreateBusinessStaffAccessProvisioningRequest(seed.BusinessB, seed.StaffActiveUserId, "Conflict", "Staff", "Active", null),
                seed.AdminUserId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "userId" && error.ErrorMessage.Contains("already has active staff access", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateAccessProvisioningAsync_CrossBusinessActivationConflict_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProvisioningScenarioAsync(dbContext);
        var service = new LoveBrackleyService(dbContext);

        dbContext.BusinessStaffAccesses.Add(new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444471"),
            BusinessProfileId = seed.BusinessA,
            UserId = seed.StaffActiveUserId,
            DisplayName = "Existing Active",
            BusinessAccessRole = "Staff",
            AccessStatus = "Active",
            PinHash = "hash",
            PinStatus = "Active",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });

        var target = new BusinessStaffAccessEntity
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444472"),
            BusinessProfileId = seed.BusinessB,
            UserId = null,
            DisplayName = "Target",
            BusinessAccessRole = "Staff",
            AccessStatus = "Suspended",
            PinHash = "hash",
            PinStatus = "ResetRequired",
            LastActivitySummary = string.Empty,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
        dbContext.BusinessStaffAccesses.Add(target);
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAccessProvisioningAsync(
                target.Id,
                new UpdateBusinessStaffAccessProvisioningRequest(seed.StaffActiveUserId, "Active", "Cross business conflict"),
                seed.AdminUserId,
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "userId" && error.ErrorMessage.Contains("already has active staff access", StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"staff-access-provisioning-{Guid.NewGuid():N}")
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<ProvisioningSeed> SeedProvisioningScenarioAsync(ApplicationDbContext dbContext)
    {
        var businessStaffRoleId = await CreateRoleAsync(dbContext, ApplicationRoleNames.BusinessStaff);

        var adminUserId = "admin-user";
        var ownerUserId = "owner-user";
        var staffActiveUserId = "staff-active";
        var staffInactiveUserId = "staff-inactive";
        var staffNoRoleUserId = "staff-no-role";

        dbContext.Users.AddRange(
            CreateUser(adminUserId, "admin@example.com", "Active"),
            CreateUser(ownerUserId, "owner@example.com", "Active"),
            CreateUser(staffActiveUserId, "staff.active@example.com", "Active"),
            CreateUser(staffInactiveUserId, "staff.inactive@example.com", "Inactive"),
            CreateUser(staffNoRoleUserId, "staff.norole@example.com", "Active"));
        await dbContext.SaveChangesAsync();

        await AssignRoleAsync(dbContext, staffActiveUserId, businessStaffRoleId);
        await AssignRoleAsync(dbContext, staffInactiveUserId, businessStaffRoleId);

        var businessA = Guid.Parse("22222222-2222-2222-2222-222222222271");
        var businessB = Guid.Parse("22222222-2222-2222-2222-222222222272");

        dbContext.BusinessProfiles.AddRange(
            CreateBusiness(businessA, ownerUserId, "Business A"),
            CreateBusiness(businessB, ownerUserId, "Business B"));

        await dbContext.SaveChangesAsync();

        return new ProvisioningSeed(adminUserId, businessA, businessB, staffActiveUserId, staffInactiveUserId, staffNoRoleUserId);
    }

    private static ApplicationUser CreateUser(string id, string email, string status)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            DisplayName = email,
            Status = status,
        };
    }

    private static BusinessProfileEntity CreateBusiness(Guid id, string ownerUserId, string name)
    {
        return new BusinessProfileEntity
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Name = name,
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
            ParticipationStatus = "Active",
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

    private sealed record ProvisioningSeed(
        string AdminUserId,
        Guid BusinessA,
        Guid BusinessB,
        string StaffActiveUserId,
        string StaffInactiveUserId,
        string StaffNoRoleUserId);
}
