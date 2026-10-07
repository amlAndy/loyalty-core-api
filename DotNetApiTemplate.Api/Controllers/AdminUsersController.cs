using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests.Admin;
using DotNetApiTemplate.Api.Contracts.Responses.Admin;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Admin.Users;
using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminAccess)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IMapper mapper, IAuthorizationService authorizationService, IAdminUserService adminUserService)
    {
        _mapper = mapper;
        _authorizationService = authorizationService;
        _adminUserService = adminUserService;
    }

    [Authorize(Policy = AdminPermissionPolicies.UsersDelete)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _adminUserService.DeleteAsync(id, cancellationToken);

            return deleted ? NoContent() : NotFound();
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

    [Authorize(Policy = AdminPermissionPolicies.UsersCreate)]
    [HttpPost]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminUserDetailDto>> Create(
        [FromBody] AdminUserUpsertDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = _mapper.Map<AdminUserUpsertModel>(request);

            if (model.RoleIds.Length > 0 && !await HasPermissionAsync(AdminPermissionPolicies.RolesAssign, cancellationToken))
            {
                return Forbid();
            }

            if (RequiresStatusPermissionOnCreate(model.Status) && !await HasPermissionAsync(AdminPermissionPolicies.UsersDeactivate, cancellationToken))
            {
                return Forbid();
            }

            var result = await _adminUserService.CreateAsync(model, cancellationToken);

            return Ok(_mapper.Map<AdminUserDetailDto>(result));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

    [Authorize(Policy = AdminPermissionPolicies.UsersView)]
    [HttpGet("form-options")]
    [ProducesResponseType(typeof(AdminUserFormOptionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminUserFormOptionsDto>> GetFormOptions(
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.GetFormOptionsAsync(cancellationToken);

        return Ok(_mapper.Map<AdminUserFormOptionsDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.UsersView)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailDto>> GetById(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var user = await _adminUserService.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<AdminUserDetailDto>(user));
    }

    [Authorize(Policy = AdminPermissionPolicies.UsersUpdate)]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailDto>> Update(
        [FromRoute] string id,
        [FromBody] AdminUserUpsertDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingUser = await _adminUserService.GetByIdAsync(id, cancellationToken);

            if (existingUser is null)
            {
                return NotFound();
            }

            var model = _mapper.Map<AdminUserUpsertModel>(request);

            if (RoleIdsChanged(existingUser.RoleIds, model.RoleIds) && !await HasPermissionAsync(AdminPermissionPolicies.RolesAssign, cancellationToken))
            {
                return Forbid();
            }

            var statusPermission = GetStatusTransitionPermission(existingUser.Status, model.Status);
            if (statusPermission is not null && !await HasPermissionAsync(statusPermission, cancellationToken))
            {
                return Forbid();
            }

            var result = await _adminUserService.UpdateAsync(id, model, cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<AdminUserDetailDto>(result));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

    [Authorize(Policy = AdminPermissionPolicies.UsersView)]
    [HttpPost("search")]
    [ProducesResponseType(typeof(AdminUserSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminUserSearchResponseDto>> SearchUsers(
        [FromBody] AdminUserSearchCriteriaDto request,
        CancellationToken cancellationToken)
    {
        var criteria = _mapper.Map<AdminUserSearchCriteria>(request);
        var result = await _adminUserService.SearchUsersAsync(criteria, cancellationToken);

        return Ok(_mapper.Map<AdminUserSearchResponseDto>(result));
    }

    private async Task<bool> HasPermissionAsync(string policyName, CancellationToken cancellationToken)
    {
        var result = await _authorizationService.AuthorizeAsync(User, resource: null, policyName);
        return result.Succeeded;
    }

    private static bool RoleIdsChanged(IReadOnlyCollection<string> existingRoleIds, IReadOnlyCollection<string> requestedRoleIds)
    {
        return !new HashSet<string>(existingRoleIds, StringComparer.Ordinal).SetEquals(requestedRoleIds);
    }

    private static bool RequiresStatusPermissionOnCreate(string requestedStatus)
    {
        return !string.Equals(requestedStatus, AdminUserStatuses.Active, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetStatusTransitionPermission(string currentStatus, string requestedStatus)
    {
        if (string.Equals(currentStatus, requestedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(requestedStatus, AdminUserStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return AdminPermissionPolicies.UsersActivate;
        }

        return AdminPermissionPolicies.UsersDeactivate;
    }

    private ActionResult CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "request" : error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return BadRequest(new ValidationProblemDetails(errors));
    }
}
