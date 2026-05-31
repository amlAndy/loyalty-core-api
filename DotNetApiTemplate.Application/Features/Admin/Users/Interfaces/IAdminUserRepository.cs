using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUserDetailModel> CreateAsync(AdminUserUpsertModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<string[]> GetExistingRoleIdsAsync(string[] roleIds, CancellationToken cancellationToken = default);
    Task<AdminUserFormOptionsModel> GetFormOptionsAsync(CancellationToken cancellationToken = default);
    Task<AdminUserDetailModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<AdminUserSearchResult> SearchUsersAsync(AdminUserSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<AdminUserDetailModel?> UpdateAsync(string id, AdminUserUpsertModel model, CancellationToken cancellationToken = default);
}
