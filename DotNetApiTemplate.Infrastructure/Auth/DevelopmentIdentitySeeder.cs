using System.Text.Json;
using DotNetApiTemplate.Infrastructure.Admin.Audit;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.Admin.Roles;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class DevelopmentIdentitySeeder
{
    private const string ActiveStatus = "Active";
    private const string PermissionClaimType = "permission";
    private static readonly string[] AllPermissionCodes = new StaticPermissionCatalogProvider()
        .GetPermissions()
        .Select(permission => permission.Code)
        .ToArray();

    private static readonly string[] AdministratorPermissionCodes =
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
            "role-super-user",
            "Super User",
            "Protected root role with full system access.",
            null,
            true,
            true,
            AllPermissionCodes),
        new(
            "role-administrator",
            "Administrator",
            "Template example administrator role under Super User.",
            "role-super-user",
            false,
            false,
            AdministratorPermissionCodes),
        new(
            "role-member",
            "Member",
            "Template example member role under Administrator.",
            "role-administrator",
            false,
            false,
            []),
        new(
            "role-supplier",
            "Supplier",
            "Template example supplier role under Administrator.",
            "role-administrator",
            false,
            false,
            [])
    ];

    public static readonly DevelopmentSeedUser[] SeedUsers =
    [
        new("member@example.com", "MemberPass!1", "Member User", "Member"),
        new("supplier@example.com", "SupplierPass!1", "Supplier User", "Supplier"),
        new("admin@example.com", "AdminPass!1", "Admin User", "Administrator"),
        new("superuser@example.com", "SuperUserPass!1", "SuperUser", "Super User")
    ];

    private static readonly DevelopmentSeedAuditEntry[] SeedAuditEntries =
    [
        new(
            Guid.Parse("9d7e85ca-b408-4fc4-a205-f8396f8aa001"),
            DateTimeOffset.Parse("2026-04-22T10:15:00Z"),
            "superuser@example.com",
            "SuperUser",
            "UserCreated",
            "User Created",
            "User",
            "admin@example.com",
            "Admin User",
            "Admin User",
            "Created an administrator account with Finance child role.",
            [
                new DevelopmentSeedAuditDetail(
                    0,
                    "financeRole",
                    "Finance role",
                    null,
                    JsonSerializer.Serialize("Finance child role")),
                new DevelopmentSeedAuditDetail(
                    1,
                    "roles",
                    "Roles",
                    JsonSerializer.Serialize(Array.Empty<string>()),
                    JsonSerializer.Serialize(new[] { "Administrator", "Member" }))
            ]),
        new(
            Guid.Parse("9d7e85ca-b408-4fc4-a205-f8396f8aa002"),
            DateTimeOffset.Parse("2026-04-24T14:45:00Z"),
            "superuser@example.com",
            "SuperUser",
            "RoleUpdated",
            "Role Updated",
            "Role",
            "role-administrator",
            "Administrator",
            "Administrator",
            "Updated Administrator example permissions under the protected Super User root.",
            [
                new DevelopmentSeedAuditDetail(
                    0,
                    "permissions",
                    "Permissions",
                    JsonSerializer.Serialize(new[] { "users.view", "roles.view" }),
                    JsonSerializer.Serialize(new[] { "admin.view", "users.view", "users.create", "users.update", "users.delete", "users.activate", "users.deactivate", "roles.view", "roles.create", "roles.update", "roles.assign", "permissions.view", "permissions.assign", "audit.view" }))
            ]),
        new(
            Guid.Parse("9d7e85ca-b408-4fc4-a205-f8396f8aa003"),
            DateTimeOffset.Parse("2026-04-26T08:30:00Z"),
            "admin@example.com",
            "Admin User",
            "UserStatusChanged",
            "User Status Changed",
            "User",
            "supplier@example.com",
            "Supplier User",
            "Supplier User",
            "Returned supplier account to active status for testing.",
            [
                new DevelopmentSeedAuditDetail(
                    0,
                    "status",
                    "Status",
                    JsonSerializer.Serialize("Inactive"),
                    JsonSerializer.Serialize("Active"))
            ])
    ];

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
            await EnsureUserAsync(seedUser.Email, seedUser.Password, seedUser.DisplayName, seedUser.RoleName);
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
        string email,
        string password,
        string displayName,
        string roleName)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                Status = ActiveStatus
            };

            var createResult = await _userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult);
        }
        else
        {
            user.DisplayName = displayName;
            user.Status = ActiveStatus;
            user.EmailConfirmed = true;
            user.UserName = email;
            user.Email = email;

            var updateResult = await _userManager.UpdateAsync(user);
            EnsureSucceeded(updateResult);
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
            EnsureSucceeded(addToRoleResult);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles
            .Where(currentRole =>
                !string.Equals(currentRole, roleName, StringComparison.Ordinal) &&
                SeedRoles.Any(seedRole => string.Equals(seedRole.Name, currentRole, StringComparison.Ordinal)))
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeFromRolesResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            EnsureSucceeded(removeFromRolesResult);
        }
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
        string Password,
        string DisplayName,
        string RoleName);

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
