using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;

public interface IAdminRoleHierarchyPolicyService
{
    bool WouldCreateCircularRelationship(string currentRoleId, string? parentRoleId, IReadOnlyCollection<AdminRoleDefinitionModel> roles);
    string[] GetDescendantRoleIds(string roleId, IReadOnlyCollection<AdminRoleDefinitionModel> roles);
    AdminRoleParentOptionModel[] GetAvailableParentOptions(string? currentRoleId, IReadOnlyCollection<AdminRoleDefinitionModel> roles);
    string[] GetAvailablePermissionCodes(string? parentRoleId, IReadOnlyCollection<AdminRoleDefinitionModel> roles, string[] allPermissionCodes);
    string[] GetInvalidPermissionCodes(string[] selectedPermissionCodes, string? parentRoleId, IReadOnlyCollection<AdminRoleDefinitionModel> roles, string[] allPermissionCodes);
    AdminRoleDeleteEligibilityModel GetDeleteEligibility(AdminRoleDefinitionModel role, IReadOnlyCollection<AdminRoleDefinitionModel> roles);
}
