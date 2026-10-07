using DotNetApiTemplate.Application.Features.Admin.Audit;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using FluentValidation;
using FluentValidation.Results;

namespace DotNetApiTemplate.Application.Features.Admin.Users.Services;

public sealed class AdminUserService : IAdminUserService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;

    private readonly IAuditWriter _auditWriter;
    private readonly IAdminMutationScope _adminMutationScope;
    private readonly IAdminPrivilegeEscalationGuard _adminPrivilegeEscalationGuard;
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IValidator<AdminUserUpsertModel> _adminUserUpsertModelValidator;

    public AdminUserService(
        IAuditWriter auditWriter,
        IAdminMutationScope adminMutationScope,
        IAdminPrivilegeEscalationGuard adminPrivilegeEscalationGuard,
        IAdminUserRepository adminUserRepository,
        IValidator<AdminUserUpsertModel> adminUserUpsertModelValidator)
    {
        _auditWriter = auditWriter;
        _adminMutationScope = adminMutationScope;
        _adminPrivilegeEscalationGuard = adminPrivilegeEscalationGuard;
        _adminUserRepository = adminUserRepository;
        _adminUserUpsertModelValidator = adminUserUpsertModelValidator;
    }

    public async Task<AdminUserDetailModel> CreateAsync(
        AdminUserUpsertModel model,
        CancellationToken cancellationToken = default)
    {
        return await _adminMutationScope.ExecuteAsync(async ct =>
        {
            var normalizedModel = NormalizeUpsertModel(model);

            await _adminUserUpsertModelValidator.ValidateAndThrowAsync(normalizedModel, ct);

            if (await _adminUserRepository.EmailExistsAsync(normalizedModel.Email, cancellationToken: ct))
            {
                throw CreateValidationException("Email", "Email is already in use.");
            }

            await EnsureRoleIdsExistAsync(normalizedModel.RoleIds, ct);
            await _adminPrivilegeEscalationGuard.EnsureUserRoleAssignmentsWithinAuthorityAsync(Array.Empty<string>(), normalizedModel.RoleIds, ct);

            var createdUser = await _adminUserRepository.CreateAsync(normalizedModel, ct);

            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.UserCreated,
                ActionLabel = "User Created",
                EntityType = AuditEntityTypes.User,
                TargetId = createdUser.Id,
                TargetName = createdUser.DisplayName,
                Target = createdUser.DisplayName,
                Summary = $"Created user {createdUser.DisplayName}.",
                Details =
                [
                    CreateDetail("displayName", "Display name", null, createdUser.DisplayName),
                    CreateDetail("email", "Email", null, createdUser.Email),
                    CreateDetail("status", "Status", null, createdUser.Status),
                    CreateDetail("roles", "Roles", Array.Empty<string>(), createdUser.Roles)
                ]
            }, ct);

            return createdUser;
        }, cancellationToken);
    }

    public Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult(false);
        }

        return _adminMutationScope.ExecuteAsync(async ct =>
        {
            var existingUser = await _adminUserRepository.GetByIdAsync(id, ct);

            if (existingUser is null)
            {
                return false;
            }

            var deleted = await _adminUserRepository.DeleteAsync(id, ct);

            if (!deleted)
            {
                return false;
            }

            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.UserDeleted,
                ActionLabel = "User Deleted",
                EntityType = AuditEntityTypes.User,
                TargetId = existingUser.Id,
                TargetName = existingUser.DisplayName,
                Target = existingUser.DisplayName,
                Summary = $"Deleted user {existingUser.DisplayName}.",
                Details =
                [
                    CreateDetail("displayName", "Display name", existingUser.DisplayName, null),
                    CreateDetail("email", "Email", existingUser.Email, null),
                    CreateDetail("status", "Status", existingUser.Status, null),
                    CreateDetail("roles", "Roles", existingUser.Roles, Array.Empty<string>())
                ]
            }, ct);

            return true;
        }, cancellationToken);
    }

    public Task<AdminUserFormOptionsModel> GetFormOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        return _adminUserRepository.GetFormOptionsAsync(cancellationToken);
    }

    public Task<AdminUserDetailModel?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<AdminUserDetailModel?>(null);
        }

        return _adminUserRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<AdminUserSearchResult> SearchUsersAsync(
        AdminUserSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var normalizedCriteria = new AdminUserSearchCriteria
        {
            Query = criteria.Query,
            RoleId = criteria.RoleId,
            Status = criteria.Status,
            PageNumber = criteria.PageNumber.GetValueOrDefault() > 0
                ? criteria.PageNumber
                : DefaultPageNumber,
            PageSize = criteria.PageSize.GetValueOrDefault() > 0
                ? criteria.PageSize
                : DefaultPageSize
        };

        return _adminUserRepository.SearchUsersAsync(normalizedCriteria, cancellationToken);
    }

    public async Task<AdminUserDetailModel?> UpdateAsync(
        string id,
        AdminUserUpsertModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return await _adminMutationScope.ExecuteAsync(async ct =>
        {
            var before = await _adminUserRepository.GetByIdAsync(id, ct);

            if (before is null)
            {
                return null;
            }

            var normalizedModel = NormalizeUpsertModel(model);

            await _adminUserUpsertModelValidator.ValidateAndThrowAsync(normalizedModel, ct);

            if (await _adminUserRepository.EmailExistsAsync(normalizedModel.Email, id, ct))
            {
                throw CreateValidationException("Email", "Email is already in use.");
            }

            await EnsureRoleIdsExistAsync(normalizedModel.RoleIds, ct);
            await _adminPrivilegeEscalationGuard.EnsureUserRoleAssignmentsWithinAuthorityAsync(before.RoleIds, normalizedModel.RoleIds, ct);

            var updatedUser = await _adminUserRepository.UpdateAsync(id, normalizedModel, ct);

            if (updatedUser is null)
            {
                return null;
            }

            await WriteUserUpdateAuditEntriesAsync(before, updatedUser, ct);

            return updatedUser;
        }, cancellationToken);
    }

    private async Task WriteUserUpdateAuditEntriesAsync(
        AdminUserDetailModel before,
        AdminUserDetailModel after,
        CancellationToken cancellationToken)
    {
        var scalarDetails = new List<AuditWriteDetailModel>();

        AddDetailIfChanged(scalarDetails, "displayName", "Display name", before.DisplayName, after.DisplayName);
        AddDetailIfChanged(scalarDetails, "email", "Email", before.Email, after.Email);
        AddDetailIfChanged(scalarDetails, "status", "Status", before.Status, after.Status);

        if (scalarDetails.Count > 0)
        {
            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.UserUpdated,
                ActionLabel = "User Updated",
                EntityType = AuditEntityTypes.User,
                TargetId = after.Id,
                TargetName = after.DisplayName,
                Target = after.DisplayName,
                Summary = $"Updated user {after.DisplayName}.",
                Details = scalarDetails.ToArray()
            }, cancellationToken);
        }

        if (!string.Equals(before.Status, after.Status, StringComparison.Ordinal))
        {
            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.UserStatusChanged,
                ActionLabel = "User Status Changed",
                EntityType = AuditEntityTypes.User,
                TargetId = after.Id,
                TargetName = after.DisplayName,
                Target = after.DisplayName,
                Summary = $"Changed status for {after.DisplayName} from {before.Status} to {after.Status}.",
                Details = [CreateDetail("status", "Status", before.Status, after.Status)]
            }, cancellationToken);
        }

        var addedRoles = after.Roles.Except(before.Roles, StringComparer.Ordinal).OrderBy(role => role, StringComparer.Ordinal).ToArray();
        foreach (var role in addedRoles)
        {
            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.UserRoleAssigned,
                ActionLabel = "User Role Assigned",
                EntityType = AuditEntityTypes.User,
                TargetId = after.Id,
                TargetName = after.DisplayName,
                Target = after.DisplayName,
                Summary = $"Assigned role {role} to {after.DisplayName}.",
                Details = [CreateDetail("roles", "Roles", Array.Empty<string>(), new[] { role })]
            }, cancellationToken);
        }

        var removedRoles = before.Roles.Except(after.Roles, StringComparer.Ordinal).OrderBy(role => role, StringComparer.Ordinal).ToArray();
        foreach (var role in removedRoles)
        {
            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.UserRoleUnassigned,
                ActionLabel = "User Role Unassigned",
                EntityType = AuditEntityTypes.User,
                TargetId = after.Id,
                TargetName = after.DisplayName,
                Target = after.DisplayName,
                Summary = $"Removed role {role} from {after.DisplayName}.",
                Details = [CreateDetail("roles", "Roles", new[] { role }, Array.Empty<string>())]
            }, cancellationToken);
        }
    }

    private async Task EnsureRoleIdsExistAsync(string[] roleIds, CancellationToken cancellationToken)
    {
        var existingRoleIds = await _adminUserRepository.GetExistingRoleIdsAsync(roleIds, cancellationToken);

        if (existingRoleIds.Length != roleIds.Length)
        {
            throw CreateValidationException("RoleIds", "One or more roleIds are invalid.");
        }
    }

    private static ValidationException CreateValidationException(string propertyName, string errorMessage)
    {
        return new ValidationException([new ValidationFailure(propertyName, errorMessage)]);
    }

    private static AuditWriteDetailModel CreateDetail(string field, string label, object? beforeValue, object? afterValue)
    {
        return new AuditWriteDetailModel
        {
            Field = field,
            Label = label,
            BeforeValue = beforeValue,
            AfterValue = afterValue
        };
    }

    private static void AddDetailIfChanged(
        ICollection<AuditWriteDetailModel> details,
        string field,
        string label,
        string beforeValue,
        string afterValue)
    {
        if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
        {
            return;
        }

        details.Add(CreateDetail(field, label, beforeValue, afterValue));
    }

    private static AdminUserUpsertModel NormalizeUpsertModel(AdminUserUpsertModel model)
    {
        return new AdminUserUpsertModel
        {
            DisplayName = model.DisplayName.Trim(),
            Email = model.Email.Trim(),
            Status = model.Status.Trim(),
            RoleIds = model.RoleIds
                .Select(roleId => roleId.Trim())
                .Where(roleId => !string.IsNullOrWhiteSpace(roleId))
                .Distinct(StringComparer.Ordinal)
                .ToArray()
        };
    }
}
