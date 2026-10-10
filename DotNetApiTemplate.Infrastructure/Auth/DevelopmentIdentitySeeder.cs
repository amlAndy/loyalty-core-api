using DotNetApiTemplate.Infrastructure.Admin.Audit;
using DotNetApiTemplate.Infrastructure.Admin.Roles;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class DevelopmentIdentitySeeder
{
    private const string ActiveStatus = "Active";
    private const string PermissionClaimType = "permission";
    private const string LocalDevelopmentPassword = "Password123!";
    private static readonly Guid BrackleyTownId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodAndDrinkCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid RetailCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid FloristCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid BooksCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000004");
    private static readonly Guid HomeAndGiftCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid CoffeeShopCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000006");
    private static readonly Guid GroceriesCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000007");
    private static readonly string[] LegacyTemplateRoleNames =
    [
        "Super User",
        "Administrator",
        "Supplier"
    ];
    private static readonly string[] LegacyTemplateUserEmails =
    [
        "member@example.com",
        "supplier@example.com",
        "admin@example.com",
        "superuser@example.com"
    ];
    private static readonly Guid[] LegacyTemplateAuditEntryIds =
    [
        Guid.Parse("9d7e85ca-b408-4fc4-a205-f8396f8aa001"),
        Guid.Parse("9d7e85ca-b408-4fc4-a205-f8396f8aa002"),
        Guid.Parse("9d7e85ca-b408-4fc4-a205-f8396f8aa003")
    ];
    private static readonly string[] AllPermissionCodes = new StaticPermissionCatalogProvider()
        .GetPermissions()
        .Select(permission => permission.Code)
        .ToArray();

    private static readonly string[] TownAdminPermissionCodes =
    [
        "admin.view",
        "users.view",
        "users.create",
        "users.update",
        "users.delete",
        "users.activate",
        "users.deactivate",
        "roles.view",
        "roles.create",
        "roles.update",
        "roles.assign",
        "permissions.view",
        "permissions.assign",
        "audit.view"
    ];

    private static readonly DevelopmentSeedRole[] SeedRoles =
    [
        new(
            "role-platform-admin",
            ApplicationRoleNames.PlatformAdmin,
            "Platform-wide administration for Love-Brackley local development.",
            null,
            true,
            true,
            AllPermissionCodes),
        new(
            "role-town-admin",
            ApplicationRoleNames.TownAdmin,
            "Town administration role for Love-Brackley local development.",
            "role-platform-admin",
            false,
            true,
            TownAdminPermissionCodes),
        new(
            "role-business-owner",
            ApplicationRoleNames.BusinessOwner,
            "Business owner role for local development journeys.",
            null,
            false,
            true,
            []),
        new(
            "role-business-staff",
            ApplicationRoleNames.BusinessStaff,
            "Business staff role for local development journeys.",
            null,
            false,
            true,
            []),
        new(
            "role-member",
            ApplicationRoleNames.Member,
            "Love-Brackley local resident and member role.",
            null,
            false,
            true,
            []),
        new(
            "role-support-user",
            ApplicationRoleNames.SupportUser,
            "Support role reserved for local development support scenarios.",
            "role-town-admin",
            false,
            true,
            [])
    ];

    public static readonly DevelopmentSeedUser[] SeedUsers =
    [
        new(
            "member.alice@love-brackley.local",
            "Alice Member",
            [ApplicationRoleNames.Member]),
        new(
            "owner.greenpantry@love-brackley.local",
            "Grace Green Pantry Owner",
            [ApplicationRoleNames.BusinessOwner]),
        new(
            "staff.greenpantry@love-brackley.local",
            "Sam Green Pantry Staff",
            [ApplicationRoleNames.BusinessStaff]),
        new(
            "admin.platform@love-brackley.local",
            "Paula Platform Admin",
            [ApplicationRoleNames.PlatformAdmin]),
        new(
            "owner.member@love-brackley.local",
            "Oliver Owner Member",
            [ApplicationRoleNames.BusinessOwner, ApplicationRoleNames.Member]),
        new(
            "admin.member@love-brackley.local",
            "Tina Town Admin Member",
            [ApplicationRoleNames.TownAdmin, ApplicationRoleNames.Member]),
        new(
            "admin.owner.member@love-brackley.local",
            "Morgan Admin Owner Member",
            [ApplicationRoleNames.TownAdmin, ApplicationRoleNames.BusinessOwner, ApplicationRoleNames.Member])
    ];

    private static readonly DevelopmentSeedUser[] LoveBrackleySeedUsers =
    [
        new("sophie.taylor@love-brackley.local", "Sophie Taylor", [ApplicationRoleNames.Member]),
        new("olivia.carter@love-brackley.local", "Olivia Carter", [ApplicationRoleNames.Member]),
        new("daniel.hughes@love-brackley.local", "Daniel Hughes", [ApplicationRoleNames.Member]),
        new("ava.thompson@love-brackley.local", "Ava Thompson", [ApplicationRoleNames.Member]),
        new("noah.patel@love-brackley.local", "Noah Patel", [ApplicationRoleNames.Member]),
        new("owner.marketkitchen@love-brackley.local", "Harriet Market Kitchen", [ApplicationRoleNames.BusinessOwner]),
        new("owner.oldtownbooks@love-brackley.local", "Benjamin Old Town Books", [ApplicationRoleNames.BusinessOwner]),
        new("owner.florist@love-brackley.local", "Priya Love Brackley Florist", [ApplicationRoleNames.BusinessOwner]),
        new("owner.willowcoffee@love-brackley.local", "Clara Willow Coffee House", [ApplicationRoleNames.BusinessOwner]),
        new("owner.homegift@love-brackley.local", "George Home & Gift", [ApplicationRoleNames.BusinessOwner]),
        new("owner.foxpantry@love-brackley.local", "Fiona Fox Lane Pantry", [ApplicationRoleNames.BusinessOwner]),
        new("owner.riverside@love-brackley.local", "Ruth Riverside Market Kitchen", [ApplicationRoleNames.BusinessOwner]),
        new("eleanor.price@love-brackley.local", "Eleanor Price", [ApplicationRoleNames.BusinessStaff]),
        new("marcus.reed@love-brackley.local", "Marcus Reed", [ApplicationRoleNames.BusinessStaff]),
        new("aisha.khan@love-brackley.local", "Aisha Khan", [ApplicationRoleNames.BusinessStaff]),
        new("leo.martin@love-brackley.local", "Leo Martin", [ApplicationRoleNames.BusinessStaff]),
        // Sample business owners for pagination testing (Development-only)
        new("sample.owner01@love-brackley.local", "Sample Owner 01", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner02@love-brackley.local", "Sample Owner 02", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner03@love-brackley.local", "Sample Owner 03", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner04@love-brackley.local", "Sample Owner 04", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner05@love-brackley.local", "Sample Owner 05", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner06@love-brackley.local", "Sample Owner 06", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner07@love-brackley.local", "Sample Owner 07", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner08@love-brackley.local", "Sample Owner 08", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner09@love-brackley.local", "Sample Owner 09", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner10@love-brackley.local", "Sample Owner 10", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner11@love-brackley.local", "Sample Owner 11", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner12@love-brackley.local", "Sample Owner 12", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner13@love-brackley.local", "Sample Owner 13", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner14@love-brackley.local", "Sample Owner 14", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner15@love-brackley.local", "Sample Owner 15", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner16@love-brackley.local", "Sample Owner 16", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner17@love-brackley.local", "Sample Owner 17", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner18@love-brackley.local", "Sample Owner 18", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner19@love-brackley.local", "Sample Owner 19", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner20@love-brackley.local", "Sample Owner 20", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner21@love-brackley.local", "Sample Owner 21", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner22@love-brackley.local", "Sample Owner 22", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner23@love-brackley.local", "Sample Owner 23", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner24@love-brackley.local", "Sample Owner 24", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner25@love-brackley.local", "Sample Owner 25", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner26@love-brackley.local", "Sample Owner 26", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner27@love-brackley.local", "Sample Owner 27", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner28@love-brackley.local", "Sample Owner 28", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner29@love-brackley.local", "Sample Owner 29", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner30@love-brackley.local", "Sample Owner 30", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner31@love-brackley.local", "Sample Owner 31", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner32@love-brackley.local", "Sample Owner 32", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner33@love-brackley.local", "Sample Owner 33", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner34@love-brackley.local", "Sample Owner 34", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner35@love-brackley.local", "Sample Owner 35", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner36@love-brackley.local", "Sample Owner 36", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner37@love-brackley.local", "Sample Owner 37", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner38@love-brackley.local", "Sample Owner 38", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner39@love-brackley.local", "Sample Owner 39", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner40@love-brackley.local", "Sample Owner 40", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner41@love-brackley.local", "Sample Owner 41", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner42@love-brackley.local", "Sample Owner 42", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner43@love-brackley.local", "Sample Owner 43", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner44@love-brackley.local", "Sample Owner 44", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner45@love-brackley.local", "Sample Owner 45", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner46@love-brackley.local", "Sample Owner 46", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner47@love-brackley.local", "Sample Owner 47", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner48@love-brackley.local", "Sample Owner 48", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner49@love-brackley.local", "Sample Owner 49", [ApplicationRoleNames.BusinessOwner]),
        new("sample.owner50@love-brackley.local", "Sample Owner 50", [ApplicationRoleNames.BusinessOwner])
    ];

    private static readonly DevelopmentSeedAuditEntry[] SeedAuditEntries = [];

    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DevelopmentIdentitySeeder(
        ApplicationDbContext dbContext,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);
        await RemoveObsoleteTemplateSeedDataAsync(cancellationToken);
        await RemoveConflictingSeedRolesAsync(cancellationToken);

        foreach (var role in SeedRoles)
        {
            var existingRole = await _roleManager.FindByIdAsync(role.Id)
                ?? await _roleManager.FindByNameAsync(role.Name);

            if (existingRole is null)
            {
                var timestamp = DateTimeOffset.UtcNow;
                var roleResult = await _roleManager.CreateAsync(new ApplicationRole
                {
                    Id = role.Id,
                    Name = role.Name,
                    NormalizedName = role.Name.ToUpperInvariant(),
                    Description = role.Description,
                    ParentRoleId = role.ParentRoleId,
                    IsRootRole = role.IsRootRole,
                    IsSystemRole = role.IsSystemRole,
                    CreatedAtUtc = timestamp,
                    UpdatedAtUtc = timestamp
                });
                EnsureSucceeded(roleResult);
            }
            else
            {
                existingRole.Name = role.Name;
                existingRole.NormalizedName = role.Name.ToUpperInvariant();
                existingRole.Description = role.Description;
                existingRole.ParentRoleId = role.ParentRoleId;
                existingRole.IsRootRole = role.IsRootRole;
                existingRole.IsSystemRole = role.IsSystemRole;
                existingRole.UpdatedAtUtc = DateTimeOffset.UtcNow;

                if (existingRole.CreatedAtUtc == default)
                {
                    existingRole.CreatedAtUtc = existingRole.UpdatedAtUtc;
                }

                var updateRoleResult = await _roleManager.UpdateAsync(existingRole);
                EnsureSucceeded(updateRoleResult);
            }

            await ReplaceRolePermissionsAsync(role.Id, role.PermissionCodes, cancellationToken);
        }

        foreach (var seedUser in SeedUsers)
        {
            await EnsureUserAsync(seedUser, cancellationToken);
        }

        foreach (var seedUser in LoveBrackleySeedUsers)
        {
            await EnsureUserAsync(seedUser, cancellationToken);
        }

        await SeedAuditEntriesAsync(cancellationToken);
        await SeedLoveBrackleyAsync(cancellationToken);
    }

    private async Task RemoveConflictingSeedRolesAsync(CancellationToken cancellationToken)
    {
        var seedNames = SeedRoles
            .Select(role => role.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seedIds = SeedRoles
            .Select(role => role.Id)
            .ToHashSet(StringComparer.Ordinal);

        var conflictingRoles = await _dbContext.Roles
            .Where(role =>
                role.Name != null &&
                seedNames.Contains(role.Name) &&
                !seedIds.Contains(role.Id))
            .ToArrayAsync(cancellationToken);

        if (conflictingRoles.Length == 0)
        {
            return;
        }

        var conflictingRoleIds = conflictingRoles
            .Select(role => role.Id)
            .ToArray();
        var conflictingRoleClaims = await _dbContext.RoleClaims
            .Where(claim => conflictingRoleIds.Contains(claim.RoleId))
            .ToArrayAsync(cancellationToken);
        var conflictingUserRoles = await _dbContext.UserRoles
            .Where(userRole => conflictingRoleIds.Contains(userRole.RoleId))
            .ToArrayAsync(cancellationToken);

        if (conflictingRoleClaims.Length > 0)
        {
            _dbContext.RoleClaims.RemoveRange(conflictingRoleClaims);
        }

        if (conflictingUserRoles.Length > 0)
        {
            _dbContext.UserRoles.RemoveRange(conflictingUserRoles);
        }

        _dbContext.Roles.RemoveRange(conflictingRoles);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveObsoleteTemplateSeedDataAsync(CancellationToken cancellationToken)
    {
        var legacyNormalizedEmails = LegacyTemplateUserEmails
            .Select(email => _userManager.NormalizeEmail(email))
            .ToArray();
        var legacyUsers = await _dbContext.Users
            .Where(user => user.NormalizedEmail != null && legacyNormalizedEmails.Contains(user.NormalizedEmail))
            .ToArrayAsync(cancellationToken);

        if (legacyUsers.Length > 0)
        {
            var legacyUserIds = legacyUsers.Select(user => user.Id).ToArray();
            var legacyUserRoles = await _dbContext.UserRoles
                .Where(userRole => legacyUserIds.Contains(userRole.UserId))
                .ToArrayAsync(cancellationToken);
            var legacyUserClaims = await _dbContext.UserClaims
                .Where(userClaim => legacyUserIds.Contains(userClaim.UserId))
                .ToArrayAsync(cancellationToken);
            var legacyUserLogins = await _dbContext.UserLogins
                .Where(userLogin => legacyUserIds.Contains(userLogin.UserId))
                .ToArrayAsync(cancellationToken);
            var legacyUserTokens = await _dbContext.UserTokens
                .Where(userToken => legacyUserIds.Contains(userToken.UserId))
                .ToArrayAsync(cancellationToken);
            var legacyRefreshTokens = await _dbContext.RefreshTokens
                .Where(refreshToken => legacyUserIds.Contains(refreshToken.UserId))
                .ToArrayAsync(cancellationToken);
            var legacyPasswordResetTokens = await _dbContext.PasswordResetTokens
                .Where(passwordResetToken => legacyUserIds.Contains(passwordResetToken.UserId))
                .ToArrayAsync(cancellationToken);

            if (legacyUserRoles.Length > 0)
            {
                _dbContext.UserRoles.RemoveRange(legacyUserRoles);
            }

            if (legacyUserClaims.Length > 0)
            {
                _dbContext.UserClaims.RemoveRange(legacyUserClaims);
            }

            if (legacyUserLogins.Length > 0)
            {
                _dbContext.UserLogins.RemoveRange(legacyUserLogins);
            }

            if (legacyUserTokens.Length > 0)
            {
                _dbContext.UserTokens.RemoveRange(legacyUserTokens);
            }

            if (legacyRefreshTokens.Length > 0)
            {
                _dbContext.RefreshTokens.RemoveRange(legacyRefreshTokens);
            }

            if (legacyPasswordResetTokens.Length > 0)
            {
                _dbContext.PasswordResetTokens.RemoveRange(legacyPasswordResetTokens);
            }

            _dbContext.Users.RemoveRange(legacyUsers);
        }

        var legacyAuditDetails = await _dbContext.AuditChangeDetails
            .Where(detail => LegacyTemplateAuditEntryIds.Contains(detail.AuditEntryId))
            .ToArrayAsync(cancellationToken);
        var legacyAuditEntries = await _dbContext.AuditEntries
            .Where(entry => LegacyTemplateAuditEntryIds.Contains(entry.Id))
            .ToArrayAsync(cancellationToken);

        if (legacyAuditDetails.Length > 0)
        {
            _dbContext.AuditChangeDetails.RemoveRange(legacyAuditDetails);
        }

        if (legacyAuditEntries.Length > 0)
        {
            _dbContext.AuditEntries.RemoveRange(legacyAuditEntries);
        }

        var legacyRoles = await _dbContext.Roles
            .Where(role => role.Name != null && LegacyTemplateRoleNames.Contains(role.Name))
            .ToArrayAsync(cancellationToken);

        if (legacyRoles.Length > 0)
        {
            var legacyRoleIds = legacyRoles.Select(role => role.Id).ToArray();
            var legacyRoleClaims = await _dbContext.RoleClaims
                .Where(roleClaim => legacyRoleIds.Contains(roleClaim.RoleId))
                .ToArrayAsync(cancellationToken);
            var legacyUserRoles = await _dbContext.UserRoles
                .Where(userRole => legacyRoleIds.Contains(userRole.RoleId))
                .ToArrayAsync(cancellationToken);

            if (legacyRoleClaims.Length > 0)
            {
                _dbContext.RoleClaims.RemoveRange(legacyRoleClaims);
            }

            if (legacyUserRoles.Length > 0)
            {
                _dbContext.UserRoles.RemoveRange(legacyUserRoles);
            }

            _dbContext.Roles.RemoveRange(legacyRoles);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplaceRolePermissionsAsync(
        string roleId,
        string[] permissionCodes,
        CancellationToken cancellationToken)
    {
        var existingClaims = await _dbContext.RoleClaims
            .Where(claim => claim.RoleId == roleId && claim.ClaimType == PermissionClaimType)
            .ToArrayAsync(cancellationToken);

        if (existingClaims.Length > 0)
        {
            _dbContext.RoleClaims.RemoveRange(existingClaims);
        }

        if (permissionCodes.Length > 0)
        {
            _dbContext.RoleClaims.AddRange(permissionCodes.Select(permissionCode => new IdentityRoleClaim<string>
            {
                RoleId = roleId,
                ClaimType = PermissionClaimType,
                ClaimValue = permissionCode
            }));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUserAsync(
        DevelopmentSeedUser seedUser,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = _userManager.NormalizeEmail(seedUser.Email);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = seedUser.Email,
                Email = seedUser.Email,
                EmailConfirmed = true,
                DisplayName = seedUser.DisplayName,
                Status = ActiveStatus
            };

            var createResult = await _userManager.CreateAsync(user, LocalDevelopmentPassword);
            EnsureSucceeded(createResult);
        }
        else
        {
            user.DisplayName = seedUser.DisplayName;
            user.Status = ActiveStatus;
            user.EmailConfirmed = true;
            user.UserName = seedUser.Email;
            user.Email = seedUser.Email;

            var updateResult = await _userManager.UpdateAsync(user);
            EnsureSucceeded(updateResult);
        }

        await EnsureLocalDevelopmentPasswordAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        var desiredRoles = new HashSet<string>(seedUser.RoleNames, StringComparer.Ordinal);
        var desiredRoleNames = SeedRoles
            .Where(role => desiredRoles.Contains(role.Name))
            .Select(role => role.Name)
            .ToArray();
        var rolesToAdd = desiredRoleNames
            .Where(roleName => !currentRoles.Contains(roleName, StringComparer.Ordinal))
            .ToArray();

        if (rolesToAdd.Length > 0)
        {
            var addToRolesResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            EnsureSucceeded(addToRolesResult);
        }

        var rolesToRemove = currentRoles
            .Where(currentRole =>
                !desiredRoles.Contains(currentRole) &&
                SeedRoles.Any(seedRole => string.Equals(seedRole.Name, currentRole, StringComparison.Ordinal)))
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeFromRolesResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            EnsureSucceeded(removeFromRolesResult);
        }

        await EnsureMemberProfileAsync(user, desiredRoleNames, cancellationToken);
    }

    private async Task EnsureMemberProfileAsync(ApplicationUser user, string[] desiredRoleNames, CancellationToken cancellationToken)
    {
        var isMemberRole = desiredRoleNames.Contains(ApplicationRoleNames.Member, StringComparer.Ordinal);

        if (!isMemberRole)
        {
            return;
        }

        var memberProfile = await _dbContext.MemberProfiles
            .SingleOrDefaultAsync(profile => profile.UserId == user.Id, cancellationToken);

        if (memberProfile is null)
        {
            memberProfile = new MemberProfileEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = user.DisplayName ?? user.UserName ?? user.Email ?? string.Empty,
                Email = user.Email,
                PublicMemberReference = GenerateMemberReference(),
                CardCode = GenerateCardCode(),
                Status = ActiveStatus,
                PointsBalance = 0,
                CreatedUtc = DateTimeOffset.UtcNow,
                UpdatedUtc = DateTimeOffset.UtcNow
            };

            _dbContext.MemberProfiles.Add(memberProfile);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateMemberReference()
    {
        return $"LBM-{DateTime.UtcNow.Ticks % 10000000:D7}";
    }

    private static string GenerateCardCode()
    {
        return $"LB-CARD-{Guid.NewGuid():N}".Substring(0, 15);
    }

    private async Task EnsureLocalDevelopmentPasswordAsync(ApplicationUser user)
    {
        if (await _userManager.CheckPasswordAsync(user, LocalDevelopmentPassword))
        {
            return;
        }

        IdentityResult result;

        if (await _userManager.HasPasswordAsync(user))
        {
            var passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            result = await _userManager.ResetPasswordAsync(user, passwordResetToken, LocalDevelopmentPassword);
        }
        else
        {
            result = await _userManager.AddPasswordAsync(user, LocalDevelopmentPassword);
        }

        EnsureSucceeded(result);
    }

    private async Task SeedAuditEntriesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedEntry in SeedAuditEntries)
        {
            var actingUser = await _userManager.FindByEmailAsync(seedEntry.ActingUserEmail);
            var targetUser = await _userManager.FindByEmailAsync(seedEntry.TargetKey);
            var entry = await _dbContext.AuditEntries
                .SingleOrDefaultAsync(auditEntry => auditEntry.Id == seedEntry.Id, cancellationToken);

            if (entry is null)
            {
                entry = new AuditEntryEntity
                {
                    Id = seedEntry.Id
                };

                _dbContext.AuditEntries.Add(entry);
            }

            entry.OccurredAtUtc = seedEntry.OccurredAtUtc;
            entry.ActingUserId = actingUser?.Id;
            entry.ActingUser = seedEntry.ActingUser;
            entry.Action = seedEntry.Action;
            entry.ActionLabel = seedEntry.ActionLabel;
            entry.EntityType = seedEntry.EntityType;
            entry.TargetId = targetUser?.Id ?? seedEntry.TargetKey;
            entry.TargetName = seedEntry.TargetName;
            entry.Target = seedEntry.Target;
            entry.Summary = seedEntry.Summary;

            var existingDetails = await _dbContext.AuditChangeDetails
                .Where(detail => detail.AuditEntryId == seedEntry.Id)
                .ToArrayAsync(cancellationToken);

            if (existingDetails.Length > 0)
            {
                _dbContext.AuditChangeDetails.RemoveRange(existingDetails);
            }

            _dbContext.AuditChangeDetails.AddRange(seedEntry.Details.Select(detail => new AuditChangeDetailEntity
            {
                Id = Guid.NewGuid(),
                AuditEntryId = seedEntry.Id,
                Sequence = detail.Sequence,
                Field = detail.Field,
                Label = detail.Label,
                BeforeValueJson = detail.BeforeValueJson,
                AfterValueJson = detail.AfterValueJson
            }));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedLoveBrackleyAsync(CancellationToken cancellationToken)
    {
        var created = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        await SeedLoveBrackleyLookupsAsync(created, cancellationToken);

        var users = await _dbContext.Users.ToDictionaryAsync(user => user.Email!, user => user.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        if (await _dbContext.MemberProfiles.AnyAsync(cancellationToken))
        {
            await EnsureBusinessProfilesAsync(users, cancellationToken);
            await EnsureSampleBusinessProfilesAsync(users, cancellationToken);
            await EnsureSampleBusinessLocationsAsync(users, cancellationToken);
            await EnsureBusinessStaffAccessesAsync(users, cancellationToken);
            return;
        }
        var members = new[]
        {
            CreateMember("11111111-1111-1111-1111-111111111101", users["sophie.taylor@love-brackley.local"], "Sophie Taylor", "sophie.taylor@love-brackley.local", "LBM-1001", "LB-CARD-1001", 1285),
            CreateMember("11111111-1111-1111-1111-111111111102", users["olivia.carter@love-brackley.local"], "Olivia Carter", "olivia.carter@love-brackley.local", "LBM-1002", "LB-CARD-1002", 640),
            CreateMember("11111111-1111-1111-1111-111111111103", users["daniel.hughes@love-brackley.local"], "Daniel Hughes", "daniel.hughes@love-brackley.local", "LBM-1003", "LB-CARD-1003", 230),
            CreateMember("11111111-1111-1111-1111-111111111104", users["ava.thompson@love-brackley.local"], "Ava Thompson", "ava.thompson@love-brackley.local", "LBM-1004", "LB-CARD-1004", 940),
            CreateMember("11111111-1111-1111-1111-111111111105", users["noah.patel@love-brackley.local"], "Noah Patel", "noah.patel@love-brackley.local", "LBM-1005", "LB-CARD-1005", 515)
        };
        _dbContext.MemberProfiles.AddRange(members);
        _dbContext.LoyaltyCards.AddRange(members.Select(member => new LoyaltyCardEntity
        {
            Id = Guid.Parse(member.Id.ToString().Replace("11111111", "21111111")),
            MemberProfileId = member.Id,
            CardCode = member.CardCode,
            PublicCardReference = member.PublicMemberReference.Replace("LBM", "LBC"),
            Status = ActiveStatus,
            IsPrimary = true,
            IssuedUtc = created,
            LinkedUtc = created
        }));

        var businesses = CreateSeedBusinesses(users);
        _dbContext.BusinessProfiles.AddRange(businesses);
        _dbContext.BusinessLocations.AddRange(businesses.Select((business, index) => new BusinessLocationEntity
        {
            Id = Guid.Parse($"33333333-3333-3333-3333-33333333320{index + 1}"),
            BusinessProfileId = business.Id,
            LocationName = business.LocationName,
            AddressLine1 = business.AddressLine1,
            Town = business.Town,
            Postcode = business.Postcode,
            TillReference = $"TILL-{index + 1:00}",
            TradingHoursSummary = business.TradingHoursSummary,
            Status = ActiveStatus,
            CreatedUtc = created,
            UpdatedUtc = created
        }));

        var sampleBusinesses = CreateSampleSeedBusinesses(users);
        _dbContext.BusinessProfiles.AddRange(sampleBusinesses);
        _dbContext.BusinessLocations.AddRange(sampleBusinesses.Select((business, index) => new BusinessLocationEntity
        {
            Id = Guid.Parse($"34444444-4444-4444-4444-44444444{index + 1:D4}"),
            BusinessProfileId = business.Id,
            LocationName = business.LocationName,
            AddressLine1 = business.AddressLine1,
            Town = business.Town,
            Postcode = business.Postcode,
            TillReference = $"SAMPLE-{index + 1:D3}",
            TradingHoursSummary = business.TradingHoursSummary,
            Status = ActiveStatus,
            CreatedUtc = created,
            UpdatedUtc = created
        }));

        var staff = CreateSeedBusinessStaffAccesses(users, businesses);
        _dbContext.BusinessStaffAccesses.AddRange(staff);

        _dbContext.LoyaltyPointsRules.Add(new LoyaltyPointsRuleEntity
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555501"),
            Name = "Love Brackley default points rule",
            EarnPointsPerPound = 1,
            PencePerPoint = 1,
            MaxRedemptionPercentage = 20,
            IsActive = true,
            EffectiveFromUtc = created,
            CreatedByUserId = users["admin.platform@love-brackley.local"],
            CreatedUtc = created
        });

        _dbContext.BusinessAlerts.AddRange(
            CreateAlert("66666666-6666-6666-6666-666666666601", businesses[0].Id, "Weekly settlement pending", "Your weekly redemption settlement is queued for review.", "Info"),
            CreateAlert("66666666-6666-6666-6666-666666666602", businesses[0].Id, "PIN reset required", "Aisha Khan needs to reset their staff PIN before redeeming.", "Warning"));

        _dbContext.LoyaltyTransactions.AddRange(
            CreateTransaction("77777777-7777-7777-7777-777777777701", members[0].Id, businesses[0].Id, staff[0].Id, "Earn", 42, 42.50m, 0, "Completed", "LB-EARN-20260312-1001", new DateTimeOffset(2026, 3, 12, 12, 15, 0, TimeSpan.Zero)),
            CreateTransaction("77777777-7777-7777-7777-777777777702", members[0].Id, businesses[1].Id, staff[3].Id, "Redeem", 250, 31.20m, 2.50m, "Completed", "LB-RED-20260320-1002", new DateTimeOffset(2026, 3, 20, 15, 40, 0, TimeSpan.Zero)),
            CreateTransaction("77777777-7777-7777-7777-777777777703", members[1].Id, businesses[2].Id, null, "Earn", 35, 35.00m, 0, "Completed", "LB-EARN-20260402-1003", new DateTimeOffset(2026, 4, 2, 10, 5, 0, TimeSpan.Zero)),
            CreateTransaction("77777777-7777-7777-7777-777777777704", members[2].Id, businesses[0].Id, staff[1].Id, "Redeem", 180, 20.00m, 1.80m, "Review", "LB-RED-20260404-1004", new DateTimeOffset(2026, 4, 4, 13, 25, 0, TimeSpan.Zero)),
            CreateTransaction("77777777-7777-7777-7777-777777777705", members[3].Id, businesses[3].Id, null, "Earn", 18, 18.75m, 0, "Completed", "LB-EARN-20260407-1005", new DateTimeOffset(2026, 4, 7, 9, 50, 0, TimeSpan.Zero)),
            CreateTransaction("77777777-7777-7777-7777-777777777706", members[4].Id, businesses[0].Id, staff[2].Id, "Redeem", 120, 15.00m, 1.20m, "Held", "LB-RED-20260409-1006", new DateTimeOffset(2026, 4, 9, 16, 10, 0, TimeSpan.Zero), "Staff PIN reset required"));

        _dbContext.BusinessPayoutPeriods.AddRange(
            CreatePayout("88888888-8888-8888-8888-888888888801", businesses[0].Id, new DateTimeOffset(2026, 3, 16, 0, 0, 0, TimeSpan.Zero), "Paid", 42.80m, 1.28m),
            CreatePayout("88888888-8888-8888-8888-888888888802", businesses[0].Id, new DateTimeOffset(2026, 3, 23, 0, 0, 0, TimeSpan.Zero), "Pending", 28.40m, 0.85m),
            CreatePayout("88888888-8888-8888-8888-888888888803", businesses[1].Id, new DateTimeOffset(2026, 3, 23, 0, 0, 0, TimeSpan.Zero), "Review", 18.50m, 0.55m),
            CreatePayout("88888888-8888-8888-8888-888888888804", businesses[2].Id, new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero), "Held", 12.00m, 0.36m, "Bank details pending"));

        _dbContext.SecurityAuditEntries.AddRange(
            CreateSecurityAudit("99999999-9999-9999-9999-999999999901", businesses[0].Id, staff[2].Id, users["admin.platform@love-brackley.local"], "PIN reset requested", "A staff PIN reset was requested for Aisha Khan.", "Logged"),
            CreateSecurityAudit("99999999-9999-9999-9999-999999999902", businesses[1].Id, staff[3].Id, users["admin.platform@love-brackley.local"], "PIN held", "Leo Martin's PIN was held after a high-risk redemption attempt.", "Escalated"));

        _dbContext.AdminReviewCases.AddRange(
            CreateReviewCase("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", "BusinessApproval", "Open", "Medium", null, businesses[6].Id, null, null, "Review Riverside Market Kitchen", "New business approval is awaiting review."),
            CreateReviewCase("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2", "MemberSupport", "InReview", "Low", members[1].Id, null, null, null, "Olivia Carter profile support", "Member asked to confirm communication preferences."),
            CreateReviewCase("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3", "HeldRedemption", "Open", "High", members[4].Id, businesses[0].Id, Guid.Parse("77777777-7777-7777-7777-777777777706"), null, "Held redemption review", "Redemption is held because the staff PIN requires reset."),
            CreateReviewCase("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4", "AccessException", "Escalated", "High", null, businesses[1].Id, null, null, "Staff PIN exception", "A staff PIN was held for manager review."),
            CreateReviewCase("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5", "PayoutException", "Open", "Medium", null, businesses[2].Id, null, Guid.Parse("88888888-8888-8888-8888-888888888804"), "Held payout", "Payout held until bank details are confirmed."));

        _dbContext.AdminAuditLogs.Add(new AdminAuditLogEntity
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            ActorUserId = users["admin.platform@love-brackley.local"],
            EntityType = "SeedData",
            EntityId = "LoveBrackley",
            Action = "Seed",
            Summary = "Seeded deterministic Love-Brackley loyalty development data.",
            CreatedUtc = created
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBusinessProfilesAsync(IReadOnlyDictionary<string, string> users, CancellationToken cancellationToken)
    {
        var seedBusinesses = CreateSeedBusinesses(users);
        var existingBusinesses = await _dbContext.BusinessProfiles.ToListAsync(cancellationToken);

        foreach (var seededBusiness in seedBusinesses)
        {
            var existingBusiness = existingBusinesses.SingleOrDefault(candidate => candidate.Id == seededBusiness.Id)
                ?? existingBusinesses.SingleOrDefault(candidate =>
                    string.Equals(candidate.BrandReference, seededBusiness.BrandReference, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(candidate.Name, seededBusiness.Name, StringComparison.OrdinalIgnoreCase));

            if (existingBusiness is null)
            {
                _dbContext.BusinessProfiles.Add(seededBusiness);
                existingBusinesses.Add(seededBusiness);
                continue;
            }

            // Preserve existing business records on rerun; only add missing seeded businesses.
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSampleBusinessProfilesAsync(IReadOnlyDictionary<string, string> users, CancellationToken cancellationToken)
    {
        var sampleBusinesses = CreateSampleSeedBusinesses(users);
        var sampleBusinessIds = sampleBusinesses.Select(candidate => candidate.Id).ToHashSet();
        var existingSampleIds = await _dbContext.BusinessProfiles
            .Where(candidate => sampleBusinessIds.Contains(candidate.Id))
            .Select(candidate => candidate.Id)
            .ToHashSetAsync(cancellationToken);

        foreach (var sampleBusiness in sampleBusinesses)
        {
            if (existingSampleIds.Contains(sampleBusiness.Id))
            {
                continue;
            }

            _dbContext.BusinessProfiles.Add(sampleBusiness);
            existingSampleIds.Add(sampleBusiness.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSampleBusinessLocationsAsync(IReadOnlyDictionary<string, string> users, CancellationToken cancellationToken)
    {
        var sampleBusinesses = CreateSampleSeedBusinesses(users);
        var existingLocations = await _dbContext.BusinessLocations
            .Select(location => new { location.Id, location.BusinessProfileId, location.TillReference })
            .ToListAsync(cancellationToken);
        var existingLocationIds = existingLocations.Select(location => location.Id).ToHashSet();
        var existingBusinessAndTill = existingLocations
            .Select(location => $"{location.BusinessProfileId:N}|{location.TillReference}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < sampleBusinesses.Length; index++)
        {
            var sampleBusiness = sampleBusinesses[index];
            var sampleTillReference = $"SAMPLE-{index + 1:D3}";
            var locationId = Guid.Parse($"34444444-4444-4444-4444-44444444{index + 1:D4}");
            var businessAndTillKey = $"{sampleBusiness.Id:N}|{sampleTillReference}";

            if (existingLocationIds.Contains(locationId) || existingBusinessAndTill.Contains(businessAndTillKey))
            {
                continue;
            }

            _dbContext.BusinessLocations.Add(new BusinessLocationEntity
            {
                Id = locationId,
                BusinessProfileId = sampleBusiness.Id,
                LocationName = sampleBusiness.LocationName,
                AddressLine1 = sampleBusiness.AddressLine1,
                Town = sampleBusiness.Town,
                Postcode = sampleBusiness.Postcode,
                TillReference = sampleTillReference,
                TradingHoursSummary = sampleBusiness.TradingHoursSummary,
                Status = ActiveStatus,
                CreatedUtc = sampleBusiness.CreatedUtc,
                UpdatedUtc = sampleBusiness.UpdatedUtc
            });
            existingLocationIds.Add(locationId);
            existingBusinessAndTill.Add(businessAndTillKey);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBusinessStaffAccessesAsync(IReadOnlyDictionary<string, string> users, CancellationToken cancellationToken)
    {
        var seedBusinesses = CreateSeedBusinesses(users);
        var seededStaffAccesses = CreateSeedBusinessStaffAccesses(users, seedBusinesses);
        var existingStaffAccesses = await _dbContext.BusinessStaffAccesses.ToListAsync(cancellationToken);

        foreach (var seededStaffAccess in seededStaffAccesses)
        {
            var existingById = existingStaffAccesses.FirstOrDefault(candidate => candidate.Id == seededStaffAccess.Id);
            if (existingById is not null)
            {
                ApplyStaffAccessSeed(existingById, seededStaffAccess);
                continue;
            }

            var matchingBusinessAndUser = existingStaffAccesses
                .Where(candidate =>
                    candidate.BusinessProfileId == seededStaffAccess.BusinessProfileId &&
                    candidate.UserId == seededStaffAccess.UserId)
                .Take(2)
                .ToList();

            if (matchingBusinessAndUser.Count == 1)
            {
                ApplyStaffAccessSeed(matchingBusinessAndUser[0], seededStaffAccess);
                continue;
            }

            if (matchingBusinessAndUser.Count == 0)
            {
                _dbContext.BusinessStaffAccesses.Add(seededStaffAccess);
                existingStaffAccesses.Add(seededStaffAccess);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyStaffAccessSeed(BusinessStaffAccessEntity existingStaffAccess, BusinessStaffAccessEntity seededStaffAccess)
    {
        existingStaffAccess.BusinessProfileId = seededStaffAccess.BusinessProfileId;
        existingStaffAccess.UserId = seededStaffAccess.UserId;
        existingStaffAccess.DisplayName = seededStaffAccess.DisplayName;
        existingStaffAccess.BusinessAccessRole = seededStaffAccess.BusinessAccessRole;
        existingStaffAccess.PinHash = seededStaffAccess.PinHash;
        existingStaffAccess.PinStatus = seededStaffAccess.PinStatus;
        existingStaffAccess.LastActivitySummary = seededStaffAccess.LastActivitySummary;
        existingStaffAccess.LastActivityUtc = seededStaffAccess.LastActivityUtc;
        existingStaffAccess.UpdatedUtc = seededStaffAccess.UpdatedUtc;
    }

    private static BusinessProfileEntity[] CreateSeedBusinesses(IReadOnlyDictionary<string, string> users)
    {
        return
        [
            CreateBusiness("22222222-2222-2222-2222-222222222201", users["owner.marketkitchen@love-brackley.local"], "Brackley Market Kitchen", "LB-BIZ-2001", "Food & Drink", FoodAndDrinkCategoryId, "Seasonal lunches and local produce from the market square.", "Market Square", "10 Market Place", "NN13 7AB", true),
            CreateBusiness("22222222-2222-2222-2222-222222222202", users["owner.oldtownbooks@love-brackley.local"], "Old Town Books", "LB-BIZ-2002", "Books & Gifts", BooksCategoryId, "Independent bookseller with gifts, cards, and community events.", "Old Town", "4 High Street", "NN13 7DW", true),
            CreateBusiness("22222222-2222-2222-2222-222222222203", users["owner.florist@love-brackley.local"], "Love Brackley Florist", "LB-BIZ-2003", "Florist", FloristCategoryId, "Fresh flowers and arrangements for local homes and events.", "Town Centre", "18 Bridge Street", "NN13 7EP", true),
            CreateBusiness("22222222-2222-2222-2222-222222222204", users["owner.willowcoffee@love-brackley.local"], "Willow Coffee House", "LB-BIZ-2004", "Coffee", CoffeeShopCategoryId, "Neighbourhood coffee, cakes, and light brunches.", "Willow Parade", "2 Willow Road", "NN13 7FA", false),
            CreateBusiness("22222222-2222-2222-2222-222222222205", users["owner.homegift@love-brackley.local"], "Brackley Home & Gift", "LB-BIZ-2005", "Homeware", HomeAndGiftCategoryId, "Home accessories, candles, and thoughtful local gifts.", "High Street", "22 High Street", "NN13 7DS", false),
            CreateBusiness("22222222-2222-2222-2222-222222222206", users["owner.foxpantry@love-brackley.local"], "Fox Lane Pantry", "LB-BIZ-2006", "Deli", GroceriesCategoryId, "Pantry staples, deli counter, and local hampers.", "Fox Lane", "7 Fox Lane", "NN13 7FN", false),
            CreateBusiness("22222222-2222-2222-2222-222222222207", users["owner.riverside@love-brackley.local"], "Riverside Market Kitchen", "LB-BIZ-2007", "Food & Drink", FoodAndDrinkCategoryId, "Riverside breakfasts, lunches, and community suppers.", "Riverside", "1 Riverside Walk", "NN13 7GR", true),
            CreateBusiness("22222222-2222-2222-2222-222222222208", users["owner.greenpantry@love-brackley.local"], "Green Pantry", "LB-BIZ-2008", "Deli", GroceriesCategoryId, "Local pantry staples, fresh produce, and neighbourhood essentials.", "Green Pantry Corner", "15 Market Place", "NN13 7AJ", true)
        ];
    }

    private static BusinessProfileEntity[] CreateSampleSeedBusinesses(IReadOnlyDictionary<string, string> users)
    {
        var sampleOwnerEmails = Enumerable.Range(1, 50)
            .Select(i => $"sample.owner{i:D2}@love-brackley.local")
            .ToArray();

        var businesses = new List<BusinessProfileEntity>();

        var sampleBusinesses = new (string name, string category, Guid categoryId, string categoryName, string location, string address, string postcode, bool featured)[]
        {
            ("Sample: Artisan Bakery & Cafe with Fresh Daily Pastries and Specialty Breads Selection", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "Town Centre", "1 Market Place", "NN13 7AB", true),
            ("Sample: Vintage Records and Vinyl Collectables", "Retail", RetailCategoryId, "Retail", "High Street", "5 High Street", "NN13 7DW", false),
            ("Sample: Blossom Studio Flowers", "Florist", FloristCategoryId, "Florist", "Bridge Street", "12 Bridge Street", "NN13 7EP", true),
            ("Sample: The Little Book Nook", "Books", BooksCategoryId, "Books", "Old Town", "3 Church Lane", "NN13 7FA", false),
            ("Sample: Rustic Home Decor", "Home & Gift", HomeAndGiftCategoryId, "Home & Gift", "Manor Park", "8 Manor Drive", "NN13 7DS", true),
            ("Sample: Artisan Bakery & Cafe with Fresh Daily Pastries", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "Riverside", "7 Riverside Walk", "NN13 7FN", false),
            ("Sample: Gourmet Coffee Roasters", "Coffee Shop", CoffeeShopCategoryId, "Coffee Shop", "Station Road", "2 Station Road", "NN13 7GR", true),
            ("Sample: Local Produce Market & Delicatessen Specialists with Premium Selections", "Groceries", GroceriesCategoryId, "Groceries", "Park Avenue", "6 Park Avenue", "NN13 7AJ", false),
            ("Sample: Contemporary Art Gallery Space", "Retail", RetailCategoryId, "Retail", "Town Centre", "9 Market Place", "NN13 7AB", true),
            ("Sample: The Cosy Bookshop and Tea Room", "Books", BooksCategoryId, "Books", "High Street", "14 High Street", "NN13 7DW", false),
            ("Sample: Floral Design Studio", "Florist", FloristCategoryId, "Florist", "Bridge Street", "16 Bridge Street", "NN13 7EP", true),
            ("Sample: Independent Coffee House", "Coffee Shop", CoffeeShopCategoryId, "Coffee Shop", "Old Town", "11 Church Lane", "NN13 7FA", false),
            ("Sample: Premium Gift Boutique", "Home & Gift", HomeAndGiftCategoryId, "Home & Gift", "Manor Park", "10 Manor Drive", "NN13 7DS", true),
            ("Sample: Neighbourhood Deli & Pantry", "Groceries", GroceriesCategoryId, "Groceries", "Riverside", "9 Riverside Walk", "NN13 7FN", false),
            ("Sample: Family-Friendly Pizzeria and Italian Restaurant", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "Station Road", "4 Station Road", "NN13 7GR", true),
            ("Sample: Bookshop Specialising in Local History", "Books", BooksCategoryId, "Books", "Park Avenue", "13 Park Avenue", "NN13 7AJ", false),
            ("Sample: Sustainable Fashion Boutique", "Retail", RetailCategoryId, "Retail", "Town Centre", "20 Market Place", "NN13 7AB", true),
            ("Sample: Flower Arrangements & Wedding Floristry", "Florist", FloristCategoryId, "Florist", "High Street", "19 High Street", "NN13 7DW", false),
            ("Sample: Classic Italian Trattoria", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "Bridge Street", "25 Bridge Street", "NN13 7EP", true),
            ("Sample: Health & Wellness Gift Shop", "Home & Gift", HomeAndGiftCategoryId, "Home & Gift", "Old Town", "8 Church Lane", "NN13 7FA", false),
            ("Sample: Specialty Tea House", "Coffee Shop", CoffeeShopCategoryId, "Coffee Shop", "Manor Park", "15 Manor Drive", "NN13 7DS", true),
            ("Sample: Organic Grocery Store", "Groceries", GroceriesCategoryId, "Groceries", "Riverside", "12 Riverside Walk", "NN13 7FN", false),
            ("Sample: Retro Gaming Store with Collectables", "Retail", RetailCategoryId, "Retail", "Station Road", "7 Station Road", "NN13 7GR", true),
            ("Sample: Tapas Bar & Spanish Restaurant", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "Park Avenue", "18 Park Avenue", "NN13 7AJ", false),
            ("Sample: Children's Book Emporium with Reading Events", "Books", BooksCategoryId, "Books", "Town Centre", "11 Market Place", "NN13 7AB", true),
            ("Sample: Seasonal Floral Design Workshop", "Florist", FloristCategoryId, "Florist", "High Street", "22 High Street", "NN13 7DW", false),
            ("Sample: Artisan Coffee Roastery", "Coffee Shop", CoffeeShopCategoryId, "Coffee Shop", "Bridge Street", "6 Bridge Street", "NN13 7EP", true),
            ("Sample: Vintage Homewares & Antiques", "Home & Gift", HomeAndGiftCategoryId, "Home & Gift", "Old Town", "13 Church Lane", "NN13 7FA", false),
            ("Sample: Vegan & Vegetarian Cafe", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "Manor Park", "5 Manor Drive", "NN13 7DS", true),
            ("Sample: Farmers Market Produce Stand", "Groceries", GroceriesCategoryId, "Groceries", "Riverside", "3 Riverside Walk", "NN13 7FN", false),
            ("Sample: Independent Bookshop", "Books", BooksCategoryId, "Books", "Station Road", "10 Station Road", "NN13 7GR", false),
            ("Sample: Designer Homeware Showroom", "Home & Gift", HomeAndGiftCategoryId, "Home & Gift", "Park Avenue", "2 Park Avenue", "NN13 7AJ", true),
            ("Sample: Mediterranean Deli & Food Hall", "Groceries", GroceriesCategoryId, "Groceries", "Town Centre", "17 Market Place", "NN13 7AB", false),
            ("Sample: Street Food & Fast Casual Kitchen", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "High Street", "26 High Street", "NN13 7DW", true),
            ("Sample: Haberdashery & Craft Supplies", "Retail", RetailCategoryId, "Retail", "Bridge Street", "21 Bridge Street", "NN13 7EP", false),
            ("Sample: Espresso Cocktail Bar", "Coffee Shop", CoffeeShopCategoryId, "Coffee Shop", "Old Town", "7 Church Lane", "NN13 7FA", true),
            ("Sample: Gift Wrapping & Card Shop", "Home & Gift", HomeAndGiftCategoryId, "Home & Gift", "Manor Park", "20 Manor Drive", "NN13 7DS", false),
            ("Sample: Indian Restaurant & Takeaway", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "Riverside", "14 Riverside Walk", "NN13 7FN", true),
            ("Sample: Used & Rare Books Specialist", "Books", BooksCategoryId, "Books", "Station Road", "15 Station Road", "NN13 7GR", false),
            ("Sample: Eco-Friendly Gift Emporium", "Home & Gift", HomeAndGiftCategoryId, "Home & Gift", "Park Avenue", "9 Park Avenue", "NN13 7AJ", true),
            ("Sample: Local Artisan Produce Market", "Groceries", GroceriesCategoryId, "Groceries", "Town Centre", "8 Market Place", "NN13 7AB", false),
            ("Sample: Thai Cuisine & Street Food", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "High Street", "31 High Street", "NN13 7DW", true),
            ("Sample: Craft Beer Shop with Tastings", "Retail", RetailCategoryId, "Retail", "Bridge Street", "10 Bridge Street", "NN13 7EP", false),
            ("Sample: Library Coffee Lounge", "Coffee Shop", CoffeeShopCategoryId, "Coffee Shop", "Old Town", "9 Church Lane", "NN13 7FA", true),
            ("Sample: Artisan Soap & Bath Products", "Home & Gift", HomeAndGiftCategoryId, "Home & Gift", "Manor Park", "12 Manor Drive", "NN13 7DS", false),
            ("Sample: Japanese Restaurant & Ramen Bar", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "Riverside", "11 Riverside Walk", "NN13 7FN", true),
            ("Sample: Comic Books & Graphic Novels Specialist", "Books", BooksCategoryId, "Books", "Station Road", "19 Station Road", "NN13 7GR", false),
            ("Sample: Sustainable Fashion & Homeware", "Retail", RetailCategoryId, "Retail", "Park Avenue", "4 Park Avenue", "NN13 7AJ", true),
            ("Sample: Spice Market & Specialty Foods", "Groceries", GroceriesCategoryId, "Groceries", "Town Centre", "24 Market Place", "NN13 7AB", false),
            ("Sample: Brunch Spot & Breakfast Kitchen", "Food & Drink", FoodAndDrinkCategoryId, "Food & Drink", "High Street", "7 High Street", "NN13 7DW", true)
        };

        for (int i = 0; i < sampleBusinesses.Length; i++)
        {
            var business = sampleBusinesses[i];
            var ownerEmail = sampleOwnerEmails[i];
            var ownerId = users[ownerEmail];
            var sampleNum = i + 1;
            var bizId = $"22222222-2222-2222-2222-{sampleNum:D12}";
            var bizRef = $"LB-SMPL-{sampleNum:D2}";

            businesses.Add(CreateBusiness(
                bizId,
                ownerId,
                business.name,
                bizRef,
                business.categoryName,
                business.categoryId,
                business.name.Length > 80 ? "" : (business.name.EndsWith(".") || business.name.EndsWith("Selection") ? "Participating in Love Brackley loyalty programme." : ""),
                business.location,
                business.address,
                business.postcode,
                business.featured
            ));
        }

        return businesses.ToArray();
    }

    private static BusinessStaffAccessEntity[] CreateSeedBusinessStaffAccesses(IReadOnlyDictionary<string, string> users, IReadOnlyList<BusinessProfileEntity> businesses)
    {
        return
        [
            CreateStaff("44444444-4444-4444-4444-444444444401", businesses[0].Id, users["eleanor.price@love-brackley.local"], "Eleanor Price", "Manager", "Active"),
            CreateStaff("44444444-4444-4444-4444-444444444402", businesses[0].Id, users["marcus.reed@love-brackley.local"], "Marcus Reed", "Staff", "Active"),
            CreateStaff("44444444-4444-4444-4444-444444444403", businesses[0].Id, users["aisha.khan@love-brackley.local"], "Aisha Khan", "Admin", "ResetRequired"),
            CreateStaff("44444444-4444-4444-4444-444444444404", businesses[1].Id, users["leo.martin@love-brackley.local"], "Leo Martin", "Staff", "Held")
        ];
    }

    private static MemberProfileEntity CreateMember(string id, string userId, string name, string email, string reference, string cardCode, int points)
    {
        var created = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        return new MemberProfileEntity
        {
            Id = Guid.Parse(id),
            UserId = userId,
            DisplayName = name,
            Email = email,
            MobileNumber = "07700 900000",
            HomeArea = "Brackley",
            PublicMemberReference = reference,
            CardCode = cardCode,
            Status = ActiveStatus,
            PointsBalance = points,
            MarketingConsent = true,
            EmailUpdatesPreference = true,
            LocalOffersPreference = true,
            CardReminderPreference = true,
            CreatedUtc = created,
            UpdatedUtc = created
        };
    }

    private async Task SeedLoveBrackleyLookupsAsync(DateTimeOffset created, CancellationToken cancellationToken)
    {
        var town = await _dbContext.Towns.SingleOrDefaultAsync(candidate => candidate.Id == BrackleyTownId, cancellationToken);
        if (town is null)
        {
            _dbContext.Towns.Add(new TownEntity
            {
                Id = BrackleyTownId,
                Name = "Brackley",
                Slug = "brackley",
                DisplayName = "Brackley",
                IsActive = true,
                DisplayOrder = 1,
                CreatedUtc = created
            });
        }

        var categories = new[]
        {
            CreateCategory(FoodAndDrinkCategoryId, "Food and Drink", "food-and-drink", "Restaurants, cafés, delis, and hospitality.", 1, created),
            CreateCategory(RetailCategoryId, "Retail", "retail", "Independent shops and local retail services.", 2, created),
            CreateCategory(FloristCategoryId, "Florist", "florist", "Florists and flower arrangement specialists.", 3, created),
            CreateCategory(BooksCategoryId, "Books", "books", "Bookshops, cards, and reading events.", 4, created),
            CreateCategory(HomeAndGiftCategoryId, "Home and Gift", "home-and-gift", "Homeware, gifts, and local presents.", 5, created),
            CreateCategory(CoffeeShopCategoryId, "Coffee Shop", "coffee-shop", "Coffee, cakes, brunch, and light refreshments.", 6, created),
            CreateCategory(GroceriesCategoryId, "Groceries", "groceries", "Grocers, pantries, delis, and local provisions.", 7, created)
        };

        foreach (var category in categories)
        {
            if (!await _dbContext.BusinessCategories.AnyAsync(candidate => candidate.Id == category.Id, cancellationToken))
            {
                _dbContext.BusinessCategories.Add(category);
            }
        }

        var existingBusinesses = await _dbContext.BusinessProfiles.ToArrayAsync(cancellationToken);
        foreach (var business in existingBusinesses)
        {
            business.TownId = BrackleyTownId;
            business.BusinessCategoryId = GetSeedCategoryId(business.Category, business.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static BusinessCategoryEntity CreateCategory(Guid id, string name, string slug, string description, int displayOrder, DateTimeOffset created)
    {
        return new BusinessCategoryEntity
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = description,
            IsActive = true,
            DisplayOrder = displayOrder,
            CreatedUtc = created
        };
    }

    private static Guid GetSeedCategoryId(string category, string businessName)
    {
        if (category.Contains("Florist", StringComparison.OrdinalIgnoreCase)) return FloristCategoryId;
        if (category.Contains("Book", StringComparison.OrdinalIgnoreCase)) return BooksCategoryId;
        if (category.Contains("Coffee", StringComparison.OrdinalIgnoreCase)) return CoffeeShopCategoryId;
        if (category.Contains("Home", StringComparison.OrdinalIgnoreCase) || category.Contains("Gift", StringComparison.OrdinalIgnoreCase)) return HomeAndGiftCategoryId;
        if (category.Contains("Deli", StringComparison.OrdinalIgnoreCase) || category.Contains("Grocery", StringComparison.OrdinalIgnoreCase) || businessName.Contains("Pantry", StringComparison.OrdinalIgnoreCase)) return GroceriesCategoryId;
        if (category.Contains("Retail", StringComparison.OrdinalIgnoreCase)) return RetailCategoryId;
        return FoodAndDrinkCategoryId;
    }

    private static BusinessProfileEntity CreateBusiness(string id, string ownerUserId, string name, string reference, string category, Guid businessCategoryId, string description, string location, string address, string postcode, bool featured)
    {
        var created = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        return new BusinessProfileEntity
        {
            Id = Guid.Parse(id),
            OwnerUserId = ownerUserId,
            Name = name,
            BrandReference = reference,
            Category = category,
            TownId = BrackleyTownId,
            BusinessCategoryId = businessCategoryId,
            Description = description,
            LocationName = location,
            AddressLine1 = address,
            Town = "Brackley",
            Postcode = postcode,
            ContactEmail = reference.ToLowerInvariant().Replace("lb-biz-", "business") + "@love-brackley.local",
            SupportEmail = "support@love-brackley.local",
            PrimaryContactName = name,
            TradingHoursSummary = "Mon-Sat 09:00-17:00",
            ParticipationStatus = ActiveStatus,
            Featured = featured,
            CreatedUtc = created,
            UpdatedUtc = created
        };
    }

    private static BusinessStaffAccessEntity CreateStaff(string id, Guid businessId, string userId, string name, string role, string pinStatus)
    {
        var created = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        return new BusinessStaffAccessEntity
        {
            Id = Guid.Parse(id),
            BusinessProfileId = businessId,
            UserId = userId,
            DisplayName = name,
            BusinessAccessRole = role,
            PinHash = LoveBrackleyService.HashPin("1234"),
            PinStatus = pinStatus,
            LastActivitySummary = "Seeded staff access",
            LastActivityUtc = created,
            CreatedUtc = created,
            UpdatedUtc = created
        };
    }

    private static BusinessAlertEntity CreateAlert(string id, Guid businessId, string title, string message, string tone)
    {
        return new BusinessAlertEntity
        {
            Id = Guid.Parse(id),
            BusinessProfileId = businessId,
            Title = title,
            Message = message,
            Tone = tone,
            Status = "Open",
            CreatedUtc = new DateTimeOffset(2026, 4, 10, 9, 0, 0, TimeSpan.Zero)
        };
    }

    private static LoyaltyTransactionEntity CreateTransaction(string id, Guid memberId, Guid businessId, Guid? staffId, string type, int points, decimal purchaseAmount, decimal redeemedValue, string status, string reference, DateTimeOffset createdUtc, string? heldReason = null)
    {
        return new LoyaltyTransactionEntity
        {
            Id = Guid.Parse(id),
            MemberProfileId = memberId,
            BusinessProfileId = businessId,
            StaffAccessId = staffId,
            TransactionType = type,
            Points = points,
            PurchaseAmount = purchaseAmount,
            RedeemedValue = redeemedValue,
            Status = status,
            Reference = reference,
            Notes = type == "Earn" ? "Seed earn transaction" : "Seed redemption transaction",
            CreatedUtc = createdUtc,
            CompletedUtc = status == "Completed" ? createdUtc : null,
            HeldReason = heldReason
        };
    }

    private static BusinessPayoutPeriodEntity CreatePayout(string id, Guid businessId, DateTimeOffset periodStart, string status, decimal redeemedAmount, decimal fees, string? heldReason = null)
    {
        return new BusinessPayoutPeriodEntity
        {
            Id = Guid.Parse(id),
            BusinessProfileId = businessId,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodStart.AddDays(7).AddTicks(-1),
            RedeemedAmount = redeemedAmount,
            Fees = fees,
            PayoutTotal = redeemedAmount - fees,
            Status = status,
            CreatedUtc = periodStart.AddDays(7),
            PaidUtc = status == "Paid" ? periodStart.AddDays(9) : null,
            HeldReason = heldReason
        };
    }

    private static SecurityAuditEntryEntity CreateSecurityAudit(string id, Guid businessId, Guid staffId, string actorUserId, string title, string detail, string status)
    {
        return new SecurityAuditEntryEntity
        {
            Id = Guid.Parse(id),
            BusinessProfileId = businessId,
            StaffAccessId = staffId,
            ActorUserId = actorUserId,
            Title = title,
            Detail = detail,
            Status = status,
            CreatedUtc = new DateTimeOffset(2026, 4, 9, 17, 0, 0, TimeSpan.Zero),
            EscalatedUtc = status == "Escalated" ? new DateTimeOffset(2026, 4, 9, 18, 0, 0, TimeSpan.Zero) : null
        };
    }

    private static AdminReviewCaseEntity CreateReviewCase(string id, string caseType, string status, string priority, Guid? memberId, Guid? businessId, Guid? transactionId, Guid? payoutId, string title, string description)
    {
        return new AdminReviewCaseEntity
        {
            Id = Guid.Parse(id),
            CaseType = caseType,
            Status = status,
            Priority = priority,
            MemberProfileId = memberId,
            BusinessProfileId = businessId,
            LoyaltyTransactionId = transactionId,
            BusinessPayoutPeriodId = payoutId,
            Title = title,
            Description = description,
            CreatedUtc = new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero)
        };
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException(errors);
    }

    public sealed record DevelopmentSeedUser(
        string Email,
        string DisplayName,
        string[] RoleNames);

    private sealed record DevelopmentSeedRole(
        string Id,
        string Name,
        string Description,
        string? ParentRoleId,
        bool IsRootRole,
        bool IsSystemRole,
        string[] PermissionCodes);

    private sealed record DevelopmentSeedAuditEntry(
        Guid Id,
        DateTimeOffset OccurredAtUtc,
        string ActingUserEmail,
        string ActingUser,
        string Action,
        string ActionLabel,
        string EntityType,
        string TargetKey,
        string TargetName,
        string Target,
        string Summary,
        DevelopmentSeedAuditDetail[] Details);

    private sealed record DevelopmentSeedAuditDetail(
        int Sequence,
        string Field,
        string Label,
        string? BeforeValueJson,
        string? AfterValueJson);
}
