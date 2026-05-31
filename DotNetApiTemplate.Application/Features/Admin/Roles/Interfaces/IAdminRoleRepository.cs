using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;

public interface IAdminRoleRepository
{
    Task<AdminRoleDefinitionModel[]> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AdminRoleDefinitionModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<AdminRoleDefinitionModel> CreateAsync(AdminRoleUpsertModel model, CancellationToken cancellationToken = default);
    Task<AdminRoleDefinitionModel?> UpdateAsync(string id, AdminRoleUpsertModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
