using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Admin.Roles;

public sealed class AdminRoleRepository : IAdminRoleRepository
{
    private const string PermissionClaimType = "permission";

    private readonly ApplicationDbContext _dbContext;

    public AdminRoleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminRoleDefinitionModel[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Select(role => new RoleProjection(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                role.ParentRoleId,
                role.IsSystemRole,
                role.IsRootRole,
                role.CreatedAtUtc,
                role.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);
        var permissionsByRoleId = await GetPermissionsByRoleIdAsync(roles.Select(role => role.Id).ToArray(), cancellationToken);

        return roles
            .Select(role => MapRole(role, permissionsByRoleId))
            .ToArray();
    }

    public async Task<AdminRoleDefinitionModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new RoleProjection(
                candidate.Id,
                candidate.Name ?? string.Empty,
                candidate.Description,
                candidate.ParentRoleId,
                candidate.IsSystemRole,
                candidate.IsRootRole,
                candidate.CreatedAtUtc,
                candidate.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        var permissionsByRoleId = await GetPermissionsByRoleIdAsync([id], cancellationToken);
        return MapRole(role, permissionsByRoleId);
    }

    public async Task<AdminRoleDefinitionModel> CreateAsync(
        AdminRoleUpsertModel model,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var role = new ApplicationRole
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = model.Name,
            NormalizedName = model.Name.ToUpperInvariant(),
            Description = model.Description,
            ParentRoleId = model.ParentRoleId,
            IsRootRole = false,
            IsSystemRole = false,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        _dbContext.Roles.Add(role);
        _dbContext.RoleClaims.AddRange(model.Permissions.Select(permission => new IdentityRoleClaim<string>
        {
            RoleId = role.Id,
            ClaimType = PermissionClaimType,
            ClaimValue = permission
        }));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(role.Id, cancellationToken)
            ?? throw new InvalidOperationException("Created role could not be reloaded.");
    }

    public async Task<AdminRoleDefinitionModel?> UpdateAsync(
        string id,
        AdminRoleUpsertModel model,
        CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (role is null)
        {
            return null;
        }

        role.Name = model.Name;
        role.NormalizedName = model.Name.ToUpperInvariant();
        role.Description = model.Description;
        role.ParentRoleId = model.ParentRoleId;
        role.UpdatedAtUtc = DateTimeOffset.UtcNow;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        var existingClaims = await _dbContext.RoleClaims
            .Where(claim => claim.RoleId == id && claim.ClaimType == PermissionClaimType)
            .ToArrayAsync(cancellationToken);

        if (existingClaims.Length > 0)
        {
            _dbContext.RoleClaims.RemoveRange(existingClaims);
        }

        _dbContext.RoleClaims.AddRange(model.Permissions.Select(permission => new IdentityRoleClaim<string>
        {
            RoleId = id,
            ClaimType = PermissionClaimType,
            ClaimValue = permission
        }));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (role is null)
        {
            return false;
        }

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<Dictionary<string, string[]>> GetPermissionsByRoleIdAsync(
        string[] roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Length == 0)
        {
            return [];
        }

        var claims = await _dbContext.RoleClaims
            .AsNoTracking()
            .Where(claim => roleIds.Contains(claim.RoleId) && claim.ClaimType == PermissionClaimType)
            .Select(claim => new RoleClaimProjection(claim.RoleId, claim.ClaimValue ?? string.Empty))
            .ToArrayAsync(cancellationToken);

        return claims
            .GroupBy(claim => claim.RoleId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(claim => claim.ClaimValue)
                    .Where(claimValue => !string.IsNullOrWhiteSpace(claimValue))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static AdminRoleDefinitionModel MapRole(
        RoleProjection role,
        IReadOnlyDictionary<string, string[]> permissionsByRoleId)
    {
        return new AdminRoleDefinitionModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            ParentRoleId = role.ParentRoleId,
            IsSystemRole = role.IsSystemRole,
            IsRootRole = role.IsRootRole,
            Permissions = permissionsByRoleId.TryGetValue(role.Id, out var permissions)
                ? permissions
                : [],
            CreatedAtUtc = role.CreatedAtUtc,
            UpdatedAtUtc = role.UpdatedAtUtc
        };
    }

    private sealed record RoleProjection(
        string Id,
        string Name,
        string Description,
        string? ParentRoleId,
        bool IsSystemRole,
        bool IsRootRole,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    private sealed record RoleClaimProjection(string RoleId, string ClaimValue);
}
