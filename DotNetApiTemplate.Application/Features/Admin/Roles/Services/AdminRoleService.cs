using DotNetApiTemplate.Application.Features.Admin.Audit;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using FluentValidation;
using FluentValidation.Results;

namespace DotNetApiTemplate.Application.Features.Admin.Roles.Services;

public sealed class AdminRoleService : IAdminRoleService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;

    private readonly IAuditWriter _auditWriter;
    private readonly IAdminMutationScope _adminMutationScope;
    private readonly IAdminRoleRepository _adminRoleRepository;
    private readonly IAdminRoleHierarchyPolicyService _adminRoleHierarchyPolicyService;
    private readonly IPermissionCatalogProvider _permissionCatalogProvider;
    private readonly IValidator<AdminRoleUpsertModel> _adminRoleUpsertModelValidator;
    private readonly IValidator<ValidateRolePermissionsModel> _validateRolePermissionsModelValidator;

    public AdminRoleService(
        IAuditWriter auditWriter,
        IAdminMutationScope adminMutationScope,
        IAdminRoleRepository adminRoleRepository,
        IAdminRoleHierarchyPolicyService adminRoleHierarchyPolicyService,
        IPermissionCatalogProvider permissionCatalogProvider,
        IValidator<AdminRoleUpsertModel> adminRoleUpsertModelValidator,
        IValidator<ValidateRolePermissionsModel> validateRolePermissionsModelValidator)
    {
        _auditWriter = auditWriter;
        _adminMutationScope = adminMutationScope;
        _adminRoleRepository = adminRoleRepository;
        _adminRoleHierarchyPolicyService = adminRoleHierarchyPolicyService;
        _permissionCatalogProvider = permissionCatalogProvider;
        _adminRoleUpsertModelValidator = adminRoleUpsertModelValidator;
        _validateRolePermissionsModelValidator = validateRolePermissionsModelValidator;
    }

    public async Task<AdminRoleSearchResult> SearchAsync(
        AdminRoleSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = criteria.Query?.Trim();
        var pageNumber = criteria.PageNumber.GetValueOrDefault() > 0
            ? criteria.PageNumber!.Value
            : DefaultPageNumber;
        var pageSize = criteria.PageSize.GetValueOrDefault() > 0
            ? criteria.PageSize!.Value
            : DefaultPageSize;
        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);
        var filteredItems = roles
            .Where(role => string.IsNullOrWhiteSpace(normalizedQuery) || role.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(role => role.Id, StringComparer.Ordinal)
            .Select(role => MapSummary(role, roles))
            .ToArray();
        var total = filteredItems.Length;
        var items = filteredItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new AdminRoleSearchResult
        {
            Items = items,
            Total = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<AdminRoleDetailModel?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);
        var role = roles.FirstOrDefault(candidate => string.Equals(candidate.Id, id.Trim(), StringComparison.Ordinal));

        return role is null ? null : MapDetail(role, roles);
    }

    public async Task<AdminRoleDetailModel> CreateAsync(
        AdminRoleUpsertModel model,
        CancellationToken cancellationToken = default)
    {
        return await _adminMutationScope.ExecuteAsync(async ct =>
        {
            var normalizedModel = NormalizeUpsertModel(model);
            await _adminRoleUpsertModelValidator.ValidateAndThrowAsync(normalizedModel, ct);

            var roles = await _adminRoleRepository.GetAllAsync(ct);
            ValidateUpsert(normalizedModel, null, roles);

            var createdRole = await _adminRoleRepository.CreateAsync(normalizedModel, ct);
            var updatedRoles = await _adminRoleRepository.GetAllAsync(ct);
            var reloadedRole = updatedRoles.FirstOrDefault(role => string.Equals(role.Id, createdRole.Id, StringComparison.Ordinal))
                ?? createdRole;
            var detail = MapDetail(reloadedRole, updatedRoles);

            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.RoleCreated,
                ActionLabel = "Role Created",
                EntityType = AuditEntityTypes.Role,
                TargetId = detail.Id,
                TargetName = detail.Name,
                Target = detail.Name,
                Summary = $"Created role {detail.Name}.",
                Details =
                [
                    CreateDetail("name", "Name", null, detail.Name),
                    CreateDetail("description", "Description", null, detail.Description),
                    CreateDetail("parentRole", "Parent role", null, detail.ParentRoleName),
                    CreateDetail("permissions", "Permissions", Array.Empty<string>(), detail.Permissions)
                ]
            }, ct);

            return detail;
        }, cancellationToken);
    }

    public async Task<AdminRoleDetailModel?> UpdateAsync(
        string id,
        AdminRoleUpsertModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return await _adminMutationScope.ExecuteAsync(async ct =>
        {
            var normalizedId = id.Trim();
            var normalizedModel = NormalizeUpsertModel(model);
            await _adminRoleUpsertModelValidator.ValidateAndThrowAsync(normalizedModel, ct);

            var roles = await _adminRoleRepository.GetAllAsync(ct);
            var existingRole = roles.FirstOrDefault(role => string.Equals(role.Id, normalizedId, StringComparison.Ordinal));

            if (existingRole is null)
            {
                return null;
            }

            if (existingRole.IsRootRole)
            {
                throw CreateValidationException("request", "The protected root role cannot be edited.");
            }

            ValidateUpsert(normalizedModel, normalizedId, roles);
            var before = MapDetail(existingRole, roles);

            var updatedRole = await _adminRoleRepository.UpdateAsync(normalizedId, normalizedModel, ct);

            if (updatedRole is null)
            {
                return null;
            }

            var updatedRoles = await _adminRoleRepository.GetAllAsync(ct);
            var reloadedRole = updatedRoles.FirstOrDefault(role => string.Equals(role.Id, normalizedId, StringComparison.Ordinal))
                ?? updatedRole;
            var after = MapDetail(reloadedRole, updatedRoles);

            await WriteRoleUpdateAuditEntriesAsync(before, after, ct);

            return after;
        }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return await _adminMutationScope.ExecuteAsync(async ct =>
        {
            var normalizedId = id.Trim();
            var roles = await _adminRoleRepository.GetAllAsync(ct);
            var role = roles.FirstOrDefault(candidate => string.Equals(candidate.Id, normalizedId, StringComparison.Ordinal));

            if (role is null)
            {
                return false;
            }

            var eligibility = _adminRoleHierarchyPolicyService.GetDeleteEligibility(role, roles);

            if (!eligibility.CanDelete)
            {
                throw CreateValidationException("request", eligibility.Reason ?? "This role cannot be deleted.");
            }

            var before = MapDetail(role, roles);
            var deleted = await _adminRoleRepository.DeleteAsync(normalizedId, ct);

            if (!deleted)
            {
                return false;
            }

            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.RoleDeleted,
                ActionLabel = "Role Deleted",
                EntityType = AuditEntityTypes.Role,
                TargetId = before.Id,
                TargetName = before.Name,
                Target = before.Name,
                Summary = $"Deleted role {before.Name}.",
                Details =
                [
                    CreateDetail("name", "Name", before.Name, null),
                    CreateDetail("description", "Description", before.Description, null),
                    CreateDetail("parentRole", "Parent role", before.ParentRoleName, null),
                    CreateDetail("permissions", "Permissions", before.Permissions, Array.Empty<string>())
                ]
            }, ct);

            return true;
        }, cancellationToken);
    }

    private async Task WriteRoleUpdateAuditEntriesAsync(
        AdminRoleDetailModel before,
        AdminRoleDetailModel after,
        CancellationToken cancellationToken)
    {
        var scalarDetails = new List<AuditWriteDetailModel>();

        AddDetailIfChanged(scalarDetails, "name", "Name", before.Name, after.Name);
        AddDetailIfChanged(scalarDetails, "description", "Description", before.Description, after.Description);
        AddDetailIfChanged(scalarDetails, "parentRole", "Parent role", before.ParentRoleName, after.ParentRoleName);

        if (scalarDetails.Count > 0)
        {
            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.RoleUpdated,
                ActionLabel = "Role Updated",
                EntityType = AuditEntityTypes.Role,
                TargetId = after.Id,
                TargetName = after.Name,
                Target = after.Name,
                Summary = $"Updated role {after.Name}.",
                Details = scalarDetails.ToArray()
            }, cancellationToken);
        }

        var addedPermissions = after.Permissions.Except(before.Permissions, StringComparer.Ordinal).OrderBy(permission => permission, StringComparer.Ordinal).ToArray();
        foreach (var permission in addedPermissions)
        {
            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.RolePermissionAssigned,
                ActionLabel = "Role Permission Assigned",
                EntityType = AuditEntityTypes.Role,
                TargetId = after.Id,
                TargetName = after.Name,
                Target = after.Name,
                Summary = $"Assigned permission {permission} to role {after.Name}.",
                Details = [CreateDetail("permissions", "Permissions", Array.Empty<string>(), new[] { permission })]
            }, cancellationToken);
        }

        var removedPermissions = before.Permissions.Except(after.Permissions, StringComparer.Ordinal).OrderBy(permission => permission, StringComparer.Ordinal).ToArray();
        foreach (var permission in removedPermissions)
        {
            await _auditWriter.WriteAsync(new AuditWriteEntryModel
            {
                Action = AuditActionTypes.RolePermissionUnassigned,
                ActionLabel = "Role Permission Unassigned",
                EntityType = AuditEntityTypes.Role,
                TargetId = after.Id,
                TargetName = after.Name,
                Target = after.Name,
                Summary = $"Removed permission {permission} from role {after.Name}.",
                Details = [CreateDetail("permissions", "Permissions", new[] { permission }, Array.Empty<string>())]
            }, cancellationToken);
        }
    }

    public async Task<AdminRoleDeleteEligibilityModel?> GetDeleteEligibilityAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);
        var role = roles.FirstOrDefault(candidate => string.Equals(candidate.Id, id.Trim(), StringComparison.Ordinal));

        return role is null
            ? null
            : _adminRoleHierarchyPolicyService.GetDeleteEligibility(role, roles);
    }

    public async Task<AdminRoleParentOptionModel[]> GetAvailableParentRolesAsync(
        string? currentRoleId,
        CancellationToken cancellationToken = default)
    {
        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);
        var normalizedCurrentRoleId = NormalizeOptionalId(currentRoleId);

        return _adminRoleHierarchyPolicyService.GetAvailableParentOptions(normalizedCurrentRoleId, roles);
    }

    public async Task<string[]> GetAvailablePermissionsAsync(
        string? parentRoleId,
        CancellationToken cancellationToken = default)
    {
        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);
        var allPermissionCodes = _permissionCatalogProvider.GetPermissions()
            .Select(permission => permission.Code)
            .ToArray();

        return _adminRoleHierarchyPolicyService.GetAvailablePermissionCodes(
            NormalizeOptionalId(parentRoleId),
            roles,
            allPermissionCodes);
    }

    public async Task<string[]> ValidatePermissionsAsync(
        ValidateRolePermissionsModel model,
        CancellationToken cancellationToken = default)
    {
        var normalizedModel = NormalizeValidatePermissionsModel(model);
        await _validateRolePermissionsModelValidator.ValidateAndThrowAsync(normalizedModel, cancellationToken);

        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(normalizedModel.CurrentRoleId) &&
            _adminRoleHierarchyPolicyService.WouldCreateCircularRelationship(
                normalizedModel.CurrentRoleId,
                normalizedModel.ParentRoleId,
                roles))
        {
            return normalizedModel.Permissions;
        }

        var allPermissionCodes = _permissionCatalogProvider.GetPermissions()
            .Select(permission => permission.Code)
            .ToArray();

        return _adminRoleHierarchyPolicyService.GetInvalidPermissionCodes(
            normalizedModel.Permissions,
            normalizedModel.ParentRoleId,
            roles,
            allPermissionCodes);
    }

    public async Task<AdminRoleSummaryModel[]> ListChildRolesAsync(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return [];
        }

        var normalizedRoleId = roleId.Trim();
        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);

        return roles
            .Where(role => string.Equals(role.ParentRoleId, normalizedRoleId, StringComparison.Ordinal))
            .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(role => role.Id, StringComparer.Ordinal)
            .Select(role => MapSummary(role, roles))
            .ToArray();
    }

    public async Task<AdminRoleFormOptionsModel> GetFormOptionsAsync(
        string? currentRoleId,
        string? selectedParentRoleId,
        CancellationToken cancellationToken = default)
    {
        var roles = await _adminRoleRepository.GetAllAsync(cancellationToken);
        var permissions = _permissionCatalogProvider.GetPermissions();
        var availablePermissionCodes = _adminRoleHierarchyPolicyService.GetAvailablePermissionCodes(
            NormalizeOptionalId(selectedParentRoleId),
            roles,
            permissions.Select(permission => permission.Code).ToArray());
        var allowedCodes = new HashSet<string>(availablePermissionCodes, StringComparer.Ordinal);

        // TODO: Confirm how multiple protected root roles should be represented if production data allows more than one.
        var protectedRootRoleId = roles
            .Where(role => role.IsRootRole)
            .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(role => role.Id, StringComparer.Ordinal)
            .Select(role => role.Id)
            .FirstOrDefault();

        return new AdminRoleFormOptionsModel
        {
            AvailableParentRoles = _adminRoleHierarchyPolicyService.GetAvailableParentOptions(NormalizeOptionalId(currentRoleId), roles),
            Permissions = permissions,
            AvailablePermissions = permissions
                .Where(permission => allowedCodes.Contains(permission.Code))
                .ToArray(),
            ProtectedRootRoleId = protectedRootRoleId
        };
    }

    private void ValidateUpsert(
        AdminRoleUpsertModel model,
        string? existingRoleId,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles)
    {
        if (string.IsNullOrWhiteSpace(model.ParentRoleId) ||
            !roles.Any(role => string.Equals(role.Id, model.ParentRoleId, StringComparison.Ordinal)))
        {
            throw CreateValidationException("ParentRoleId", "A parent role is required.");
        }

        if (!string.IsNullOrWhiteSpace(existingRoleId) &&
            _adminRoleHierarchyPolicyService.WouldCreateCircularRelationship(existingRoleId, model.ParentRoleId, roles))
        {
            throw CreateValidationException("ParentRoleId", "A role cannot be assigned to itself or one of its child roles.");
        }

        var duplicateName = roles.Any(role =>
            !string.Equals(role.Id, existingRoleId, StringComparison.Ordinal) &&
            string.Equals(role.Name, model.Name, StringComparison.OrdinalIgnoreCase));

        if (duplicateName)
        {
            throw CreateValidationException("Name", "A role with this name already exists.");
        }

        var allPermissionCodes = _permissionCatalogProvider.GetPermissions()
            .Select(permission => permission.Code)
            .ToArray();
        var invalidPermissions = _adminRoleHierarchyPolicyService.GetInvalidPermissionCodes(
            model.Permissions,
            model.ParentRoleId,
            roles,
            allPermissionCodes);

        if (invalidPermissions.Length > 0)
        {
            throw CreateValidationException("Permissions", "Selected permissions must be allowed by the parent role.");
        }
    }

    private static AdminRoleSummaryModel MapSummary(
        AdminRoleDefinitionModel role,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles)
    {
        var parentRole = string.IsNullOrWhiteSpace(role.ParentRoleId)
            ? null
            : roles.FirstOrDefault(candidate => string.Equals(candidate.Id, role.ParentRoleId, StringComparison.Ordinal));

        return new AdminRoleSummaryModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            ParentRoleId = role.ParentRoleId,
            IsSystemRole = role.IsSystemRole,
            IsRootRole = role.IsRootRole,
            Permissions = role.Permissions,
            CreatedAt = role.CreatedAtUtc.ToString("O"),
            UpdatedAt = role.UpdatedAtUtc.ToString("O"),
            ParentRoleName = parentRole?.Name,
            ChildCount = roles.Count(candidate => string.Equals(candidate.ParentRoleId, role.Id, StringComparison.Ordinal)),
            IsProtected = role.IsRootRole
        };
    }

    private static AdminRoleDetailModel MapDetail(
        AdminRoleDefinitionModel role,
        IReadOnlyCollection<AdminRoleDefinitionModel> roles)
    {
        return new AdminRoleDetailModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            ParentRoleId = role.ParentRoleId,
            IsSystemRole = role.IsSystemRole,
            IsRootRole = role.IsRootRole,
            Permissions = role.Permissions,
            CreatedAt = role.CreatedAtUtc.ToString("O"),
            UpdatedAt = role.UpdatedAtUtc.ToString("O"),
            ParentRoleName = string.IsNullOrWhiteSpace(role.ParentRoleId)
                ? null
                : roles.FirstOrDefault(candidate => string.Equals(candidate.Id, role.ParentRoleId, StringComparison.Ordinal))?.Name,
            ChildCount = roles.Count(candidate => string.Equals(candidate.ParentRoleId, role.Id, StringComparison.Ordinal)),
            IsProtected = role.IsRootRole,
            ChildRoles = roles
                .Where(candidate => string.Equals(candidate.ParentRoleId, role.Id, StringComparison.Ordinal))
                .OrderBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
                .Select(candidate => new AdminRoleChildModel
                {
                    Id = candidate.Id,
                    Name = candidate.Name
                })
                .ToArray()
        };
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
        string? beforeValue,
        string? afterValue)
    {
        if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
        {
            return;
        }

        details.Add(CreateDetail(field, label, beforeValue, afterValue));
    }

    private static AdminRoleUpsertModel NormalizeUpsertModel(AdminRoleUpsertModel model)
    {
        return new AdminRoleUpsertModel
        {
            Name = model.Name.Trim(),
            Description = model.Description.Trim(),
            ParentRoleId = NormalizeOptionalId(model.ParentRoleId),
            Permissions = model.Permissions
                .Select(permission => permission.Trim())
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Distinct(StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static ValidateRolePermissionsModel NormalizeValidatePermissionsModel(ValidateRolePermissionsModel model)
    {
        return new ValidateRolePermissionsModel
        {
            ParentRoleId = NormalizeOptionalId(model.ParentRoleId),
            CurrentRoleId = NormalizeOptionalId(model.CurrentRoleId),
            Permissions = model.Permissions
                .Select(permission => permission.Trim())
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Distinct(StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static string? NormalizeOptionalId(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
