using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests.Admin;
using DotNetApiTemplate.Api.Contracts.Responses.Admin;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminAccess)]
public sealed class AdminRolesController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IAdminRoleService _adminRoleService;

    public AdminRolesController(IMapper mapper, IAdminRoleService adminRoleService)
    {
        _mapper = mapper;
        _adminRoleService = adminRoleService;
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(AdminRoleSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminRoleSearchResponseDto>> Search(
        [FromBody] AdminRoleSearchCriteriaDto request,
        CancellationToken cancellationToken)
    {
        var criteria = _mapper.Map<AdminRoleSearchCriteria>(request);
        var result = await _adminRoleService.SearchAsync(criteria, cancellationToken);

        return Ok(_mapper.Map<AdminRoleSearchResponseDto>(result));
    }

    [HttpGet("parent-options")]
    [ProducesResponseType(typeof(AdminRoleParentOptionDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminRoleParentOptionDto[]>> GetAvailableParentRoles(
        [FromQuery] string? currentRoleId,
        CancellationToken cancellationToken)
    {
        var result = await _adminRoleService.GetAvailableParentRolesAsync(currentRoleId, cancellationToken);

        return Ok(_mapper.Map<AdminRoleParentOptionDto[]>(result));
    }

    [HttpGet("available-permissions")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<string[]>> GetAvailablePermissions(
        [FromQuery] string? parentRoleId,
        CancellationToken cancellationToken)
    {
        var result = await _adminRoleService.GetAvailablePermissionsAsync(parentRoleId, cancellationToken);

        return Ok(result);
    }

    [HttpPost("validate-permissions")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<string[]>> ValidatePermissions(
        [FromBody] ValidateRolePermissionsRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = _mapper.Map<ValidateRolePermissionsModel>(request);
            var result = await _adminRoleService.ValidatePermissionsAsync(model, cancellationToken);

            return Ok(result);
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

    [HttpGet("form-options")]
    [ProducesResponseType(typeof(AdminRoleFormOptionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminRoleFormOptionsDto>> GetFormOptions(
        [FromQuery] string? currentRoleId,
        [FromQuery] string? selectedParentRoleId,
        CancellationToken cancellationToken)
    {
        var result = await _adminRoleService.GetFormOptionsAsync(currentRoleId, selectedParentRoleId, cancellationToken);

        return Ok(_mapper.Map<AdminRoleFormOptionsDto>(result));
    }

    [HttpGet("{id}/delete-eligibility")]
    [ProducesResponseType(typeof(AdminRoleDeleteEligibilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRoleDeleteEligibilityDto>> GetDeleteEligibility(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var result = await _adminRoleService.GetDeleteEligibilityAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<AdminRoleDeleteEligibilityDto>(result));
    }

    [HttpGet("{id}/children")]
    [ProducesResponseType(typeof(AdminRoleSummaryDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminRoleSummaryDto[]>> GetChildren(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var result = await _adminRoleService.ListChildRolesAsync(id, cancellationToken);

        return Ok(_mapper.Map<AdminRoleSummaryDto[]>(result));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AdminRoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRoleDetailDto>> GetById(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var role = await _adminRoleService.GetByIdAsync(id, cancellationToken);

        if (role is null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<AdminRoleDetailDto>(role));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminRoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminRoleDetailDto>> Create(
        [FromBody] CreateAdminRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = _mapper.Map<AdminRoleUpsertModel>(request);
            var result = await _adminRoleService.CreateAsync(model, cancellationToken);

            return Ok(_mapper.Map<AdminRoleDetailDto>(result));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AdminRoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRoleDetailDto>> Update(
        [FromRoute] string id,
        [FromBody] UpdateAdminRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = _mapper.Map<AdminRoleUpsertModel>(request);
            var result = await _adminRoleService.UpdateAsync(id, model, cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<AdminRoleDetailDto>(result));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

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
            var deleted = await _adminRoleService.DeleteAsync(id, cancellationToken);

            return deleted ? NoContent() : NotFound();
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
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
