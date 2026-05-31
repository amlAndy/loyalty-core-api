using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests.Admin;
using DotNetApiTemplate.Api.Contracts.Responses.Admin;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
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
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IMapper mapper, IAdminUserService adminUserService)
    {
        _mapper = mapper;
        _adminUserService = adminUserService;
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
            var deleted = await _adminUserService.DeleteAsync(id, cancellationToken);

            return deleted ? NoContent() : NotFound();
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

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
            var result = await _adminUserService.CreateAsync(model, cancellationToken);

            return Ok(_mapper.Map<AdminUserDetailDto>(result));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

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
            var model = _mapper.Map<AdminUserUpsertModel>(request);
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
