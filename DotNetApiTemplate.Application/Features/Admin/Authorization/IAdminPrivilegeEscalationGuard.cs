using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Authorization;

public interface IAdminPrivilegeEscalationGuard
{
    Task EnsureRoleMutationWithinAuthorityAsync(
        AdminRoleUpsertModel proposedModel,
        string? existingRoleId,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles,
        CancellationToken cancellationToken = default);

    Task EnsureUserRoleAssignmentsWithinAuthorityAsync(
        string[] existingRoleIds,
        string[] requestedRoleIds,
        CancellationToken cancellationToken = default);
}
