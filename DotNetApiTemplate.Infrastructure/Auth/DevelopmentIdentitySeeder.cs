using DotNetApiTemplate.Infrastructure.Admin.Audit;
using DotNetApiTemplate.Infrastructure.Admin.Roles;
using DotNetApiTemplate.Infrastructure.Identity;
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

        await SeedAuditEntriesAsync(cancellationToken);
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
