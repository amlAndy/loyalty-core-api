using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using DotNetApiTemplate.Application.Features.Auth.Exceptions;

namespace DotNetApiTemplate.Application.Features.Admin.Authorization;

public sealed class AdminPrivilegeEscalationGuard : IAdminPrivilegeEscalationGuard
{
    private readonly IAdminActorContext _adminActorContext;
    private readonly IAdminRoleRepository _adminRoleRepository;
    private readonly IAdminUserRepository _adminUserRepository;

    public AdminPrivilegeEscalationGuard(
        IAdminActorContext adminActorContext,
        IAdminRoleRepository adminRoleRepository,
        IAdminUserRepository adminUserRepository)
    {
        _adminActorContext = adminActorContext;
        _adminRoleRepository = adminRoleRepository;
        _adminUserRepository = adminUserRepository;
    }

    public async Task EnsureRoleMutationWithinAuthorityAsync(
        AdminRoleUpsertModel proposedModel,
        string? existingRoleId,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var authority = await GetAuthorityAsync(roles, cancellationToken);
        EnsurePermissionsWithinAuthority(proposedModel.Permissions, authority.ActorPermissions);

        if (!string.IsNullOrWhiteSpace(proposedModel.ParentRoleId))
        {
            var parentRole = FindRole(roles, proposedModel.ParentRoleId);

            if (parentRole is null || !IsRoleWithinAuthority(parentRole, authority.ActorRole, authority.ActorPermissions, authority.RolesById))
            {
                throw new AuthForbiddenException();
            }
        }

        if (!string.IsNullOrWhiteSpace(existingRoleId))
        {
            var existingRole = FindRole(roles, existingRoleId);

            if (existingRole is null || !IsRoleWithinAuthority(existingRole, authority.ActorRole, authority.ActorPermissions, authority.RolesById))
            {
                throw new AuthForbiddenException();
            }
        }
    }

    public async Task EnsureUserRoleAssignmentsWithinAuthorityAsync(
        string[] existingRoleIds,
        string[] requestedRoleIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);
        var authority = await GetAuthorityAsync(roles, cancellationToken);
        var existingRoleIdSet = new HashSet<string>(existingRoleIds, StringComparer.Ordinal);
        var requestedRoleIdSet = new HashSet<string>(requestedRoleIds, StringComparer.Ordinal);
        var changedRoleIds = requestedRoleIdSet.Except(existingRoleIdSet, StringComparer.Ordinal)
            .Concat(existingRoleIdSet.Except(requestedRoleIdSet, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var roleId in changedRoleIds)
        {
            var role = FindRole(roles, roleId);

            if (role is null || !IsRoleWithinAuthority(role, authority.ActorRole, authority.ActorPermissions, authority.RolesById))
            {
                throw new AuthForbiddenException();
            }
        }
    }

    private async Task<AdminAuthoritySnapshot> GetAuthorityAsync(
        IReadOnlyCollection<AdminRoleDefinitionModel> roles,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var activeRoleId = _adminActorContext.ActiveRoleId;
        var actorUserId = _adminActorContext.UserId;

        if (string.IsNullOrWhiteSpace(activeRoleId) || string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new AuthForbiddenException();
        }

        var actorUser = await _adminUserRepository.GetByIdAsync(actorUserId, cancellationToken);
        if (actorUser is null || !actorUser.RoleIds.Contains(activeRoleId, StringComparer.Ordinal))
        {
            throw new AuthForbiddenException();
        }

        var rolesById = roles.ToDictionary(role => role.Id, StringComparer.Ordinal);

        if (!rolesById.TryGetValue(activeRoleId, out var actorRole))
        {
            throw new AuthForbiddenException();
        }

        var actorPermissions = new HashSet<string>(actorRole.Permissions, StringComparer.Ordinal);
        return new AdminAuthoritySnapshot(actorRole, actorPermissions, rolesById);
    }

    private static void EnsurePermissionsWithinAuthority(string[] proposedPermissions, HashSet<string> actorPermissions)
    {
        if (proposedPermissions.Any(permission => !actorPermissions.Contains(permission)))
        {
            throw new AuthForbiddenException();
        }
    }

    private static AdminRoleDefinitionModel? FindRole(
        IReadOnlyCollection<AdminRoleDefinitionModel> roles,
        string roleId)
    {
        return roles.FirstOrDefault(role => string.Equals(role.Id, roleId, StringComparison.Ordinal));
    }

    private static bool IsRoleWithinAuthority(
        AdminRoleDefinitionModel role,
        AdminRoleDefinitionModel actorRole,
        HashSet<string> actorPermissions,
        IReadOnlyDictionary<string, AdminRoleDefinitionModel> rolesById)
    {
        return IsRoleWithinAuthority(role, actorRole, actorPermissions, rolesById, new HashSet<string>(StringComparer.Ordinal));
    }

    private static bool IsRoleWithinAuthority(
        AdminRoleDefinitionModel role,
        AdminRoleDefinitionModel actorRole,
        HashSet<string> actorPermissions,
        IReadOnlyDictionary<string, AdminRoleDefinitionModel> rolesById,
        HashSet<string> visited)
    {
        if (!visited.Add(role.Id))
        {
            return false;
        }

        if (string.Equals(role.Id, actorRole.Id, StringComparison.Ordinal))
        {
            return true;
        }

        if (role.IsRootRole)
        {
            return false;
        }

        if (role.Permissions.Any(permission => !actorPermissions.Contains(permission)))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(role.ParentRoleId))
        {
            return true;
        }

        if (!rolesById.TryGetValue(role.ParentRoleId, out var parentRole))
        {
            return false;
        }

        return IsRoleWithinAuthority(parentRole, actorRole, actorPermissions, rolesById, visited);
    }

    private sealed record AdminAuthoritySnapshot(
        AdminRoleDefinitionModel ActorRole,
        HashSet<string> ActorPermissions,
        IReadOnlyDictionary<string, AdminRoleDefinitionModel> RolesById);
}
