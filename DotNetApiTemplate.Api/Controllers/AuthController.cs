using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests.Auth;
using DotNetApiTemplate.Api.Contracts.Responses.Auth;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.Auth.Exceptions;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IAuthService _authService;

    public AuthController(IMapper mapper, IAuthService authService)
    {
        _mapper = mapper;
        _authService = authService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<LoginCommand>(request);
            var result = await _authService.LoginAsync(command, cancellationToken);

            if (result is null)
            {
                return Unauthorized();
            }

            return Ok(_mapper.Map<LoginResponseDto>(result));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

    [Authorize]
    [HttpPost("select-role")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthSessionDto>> SelectRole(
        [FromBody] SelectRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = GetBearerToken();

            if (accessToken is null)
            {
                return Unauthorized();
            }

            var command = _mapper.Map<SelectRoleCommand>(request);
            var session = await _authService.SelectRoleAsync(accessToken, command, cancellationToken);

            if (session is null)
            {
                return Unauthorized();
            }

            return Ok(_mapper.Map<AuthSessionDto>(session));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
        catch (AuthForbiddenException)
        {
            return Forbid();
        }
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accessToken = GetBearerToken();

        if (accessToken is null)
        {
            return Unauthorized();
        }

        var loggedOut = await _authService.LogoutAsync(accessToken, cancellationToken);

        return loggedOut ? NoContent() : Unauthorized();
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<ForgotPasswordCommand>(request);

            await _authService.ForgotPasswordAsync(command, cancellationToken);

            return NoContent();
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(AuthErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<ResetPasswordCommand>(request);

            await _authService.ResetPasswordAsync(command, cancellationToken);

            return NoContent();
        }
        catch (ValidationException exception)
        {
            return BadRequest(new AuthErrorResponseDto
            {
                Message = exception.Errors.FirstOrDefault()?.ErrorMessage ?? "Password reset failed."
            });
        }
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedUserDto>> GetMe(CancellationToken cancellationToken)
    {
        var accessToken = GetBearerToken();

        if (accessToken is null)
        {
            return Unauthorized();
        }

        var user = await _authService.GetCurrentUserAsync(accessToken, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(_mapper.Map<AuthenticatedUserDto>(user));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionDto>> Refresh(
        [FromBody] RefreshSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<RefreshSessionCommand>(request);
            var session = await _authService.RefreshAsync(command, cancellationToken);

            if (session is null)
            {
                return Unauthorized();
            }

            return Ok(_mapper.Map<AuthSessionDto>(session));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
    }

    [HttpPost("sso-login")]
    [ProducesResponseType(typeof(SsoLoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SsoLoginResultDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SsoLoginResultDto>> SsoLogin(
        [FromBody] SsoLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<SsoLoginCommand>(request);
            var result = await _authService.SsoLoginAsync(command, cancellationToken);

            return Ok(_mapper.Map<SsoLoginResultDto>(result));
        }
        catch (ValidationException exception)
        {
            return CreateValidationProblem(exception);
        }
        catch (AuthForbiddenException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new SsoLoginResultDto
                {
                    Success = false,
                    RedirectUrl = exception.RedirectUrl
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
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

    private string? GetBearerToken()
    {
        var authorization = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";

        return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..].Trim()
            : null;
    }
}
