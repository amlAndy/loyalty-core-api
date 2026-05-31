using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;

public interface IAdminRoleService
{
    Task<AdminRoleSearchResult> SearchAsync(AdminRoleSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<AdminRoleDetailModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<AdminRoleDetailModel> CreateAsync(AdminRoleUpsertModel model, CancellationToken cancellationToken = default);
    Task<AdminRoleDetailModel?> UpdateAsync(string id, AdminRoleUpsertModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<AdminRoleDeleteEligibilityModel?> GetDeleteEligibilityAsync(string id, CancellationToken cancellationToken = default);
    Task<AdminRoleParentOptionModel[]> GetAvailableParentRolesAsync(string? currentRoleId, CancellationToken cancellationToken = default);
    Task<string[]> GetAvailablePermissionsAsync(string? parentRoleId, CancellationToken cancellationToken = default);
    Task<string[]> ValidatePermissionsAsync(ValidateRolePermissionsModel model, CancellationToken cancellationToken = default);
    Task<AdminRoleSummaryModel[]> ListChildRolesAsync(string roleId, CancellationToken cancellationToken = default);
    Task<AdminRoleFormOptionsModel> GetFormOptionsAsync(string? currentRoleId, string? selectedParentRoleId, CancellationToken cancellationToken = default);
}
