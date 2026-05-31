using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;

namespace DotNetApiTemplate.Application.Features.Admin.Roles.Services;

public sealed class AdminRoleHierarchyPolicyService : IAdminRoleHierarchyPolicyService
{
    public bool WouldCreateCircularRelationship(
        string currentRoleId,
        string? parentRoleId,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles)
    {
        if (string.IsNullOrWhiteSpace(currentRoleId) || string.IsNullOrWhiteSpace(parentRoleId))
        {
            return false;
        }

        if (string.Equals(currentRoleId, parentRoleId, StringComparison.Ordinal))
        {
            return true;
        }

        return GetDescendantRoleIds(currentRoleId, roles).Contains(parentRoleId, StringComparer.Ordinal);
    }

    public string[] GetDescendantRoleIds(string roleId, IReadOnlyCollection<AdminRoleDefinitionModel> roles)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return [];
        }

        var descendants = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(roleId);

        while (queue.Count > 0)
        {
            var currentRoleId = queue.Dequeue();
            var childRoleIds = roles
                .Where(role => string.Equals(role.ParentRoleId, currentRoleId, StringComparison.Ordinal))
                .Select(role => role.Id)
                .ToArray();

            foreach (var childRoleId in childRoleIds)
            {
                if (!descendants.Add(childRoleId))
                {
                    continue;
                }

                queue.Enqueue(childRoleId);
            }
        }

        return descendants.ToArray();
    }

    public AdminRoleParentOptionModel[] GetAvailableParentOptions(
        string? currentRoleId,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles)
    {
        string[] blockedRoleIds = string.IsNullOrWhiteSpace(currentRoleId)
            ? []
            : [currentRoleId, .. GetDescendantRoleIds(currentRoleId, roles)];

        return roles
            .Where(role => !blockedRoleIds.Contains(role.Id, StringComparer.Ordinal))
            .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(role => role.Id, StringComparer.Ordinal)
            .Select(role => new AdminRoleParentOptionModel
            {
                Id = role.Id,
                Name = role.Name,
                ParentRoleId = role.ParentRoleId,
                IsRootRole = role.IsRootRole,
                IsSystemRole = role.IsSystemRole
            })
            .ToArray();
    }

    public string[] GetAvailablePermissionCodes(
        string? parentRoleId,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles,
        string[] allPermissionCodes)
    {
        if (string.IsNullOrWhiteSpace(parentRoleId))
        {
            return [.. allPermissionCodes];
        }

        var parentRole = roles.FirstOrDefault(role => string.Equals(role.Id, parentRoleId, StringComparison.Ordinal));

        if (parentRole is null)
        {
            return [];
        }

        var allowedPermissions = new HashSet<string>(parentRole.Permissions, StringComparer.Ordinal);

        return allPermissionCodes
            .Where(permissionCode => allowedPermissions.Contains(permissionCode))
            .ToArray();
    }

    public string[] GetInvalidPermissionCodes(
        string[] selectedPermissionCodes,
        string? parentRoleId,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles,
        string[] allPermissionCodes)
    {
        if (selectedPermissionCodes.Length == 0)
        {
            return [];
        }

        var allowedPermissionCodes = new HashSet<string>(
            GetAvailablePermissionCodes(parentRoleId, roles, allPermissionCodes),
            StringComparer.Ordinal);

        return selectedPermissionCodes
            .Where(permissionCode => !allowedPermissionCodes.Contains(permissionCode))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public AdminRoleDeleteEligibilityModel GetDeleteEligibility(
        AdminRoleDefinitionModel role,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles)
    {
        if (role.IsRootRole)
        {
            return new AdminRoleDeleteEligibilityModel
            {
                CanDelete = false,
                Reason = "Super User cannot be deleted."
            };
        }

        if (role.IsSystemRole)
        {
            return new AdminRoleDeleteEligibilityModel
            {
                CanDelete = false,
                Reason = "This protected system role cannot be deleted."
            };
        }

        if (roles.Any(candidate => string.Equals(candidate.ParentRoleId, role.Id, StringComparison.Ordinal)))
        {
            return new AdminRoleDeleteEligibilityModel
            {
                CanDelete = false,
                Reason = "This role cannot be deleted because it has child roles. Delete or reassign the child roles before deleting this role."
            };
        }

        return new AdminRoleDeleteEligibilityModel
        {
            CanDelete = true,
            Reason = null
        };
    }
}
