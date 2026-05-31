using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.Auth.Exceptions;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using FluentValidation;
using FluentValidation.Results;

namespace DotNetApiTemplate.Application.Features.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<ForgotPasswordCommand> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordCommand> _resetPasswordValidator;
    private readonly IValidator<RefreshSessionCommand> _refreshSessionValidator;
    private readonly IValidator<SelectRoleCommand> _selectRoleValidator;
    private readonly IValidator<SsoLoginCommand> _ssoLoginValidator;
    private readonly IUserAuthRepository _userAuthRepository;
    private readonly IPasswordResetLinkBuilder _passwordResetLinkBuilder;
    private readonly IPasswordResetEmailSender _passwordResetEmailSender;
    private readonly ISsoTokenProcessor _ssoTokenProcessor;

    public AuthService(
        IValidator<LoginCommand> loginValidator,
        IValidator<ForgotPasswordCommand> forgotPasswordValidator,
        IValidator<ResetPasswordCommand> resetPasswordValidator,
        IValidator<RefreshSessionCommand> refreshSessionValidator,
        IValidator<SelectRoleCommand> selectRoleValidator,
        IValidator<SsoLoginCommand> ssoLoginValidator,
        IUserAuthRepository userAuthRepository,
        IPasswordResetLinkBuilder passwordResetLinkBuilder,
        IPasswordResetEmailSender passwordResetEmailSender,
        ISsoTokenProcessor ssoTokenProcessor)
    {
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _refreshSessionValidator = refreshSessionValidator;
        _selectRoleValidator = selectRoleValidator;
        _ssoLoginValidator = ssoLoginValidator;
        _userAuthRepository = userAuthRepository;
        _passwordResetLinkBuilder = passwordResetLinkBuilder;
        _passwordResetEmailSender = passwordResetEmailSender;
        _ssoTokenProcessor = ssoTokenProcessor;
    }

    public async Task<LoginResult?> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(command, cancellationToken);

        return await _userAuthRepository.AuthenticateAsync(
            command.Email,
            command.Password,
            cancellationToken);
    }

    public Task<bool> LogoutAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(false);
        }

        return _userAuthRepository.LogoutAsync(accessToken, cancellationToken);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        await _forgotPasswordValidator.ValidateAndThrowAsync(command, cancellationToken);

        var request = await _userAuthRepository.CreatePasswordResetRequestAsync(command.Email, cancellationToken);

        if (request is null)
        {
            return;
        }

        var resetLink = _passwordResetLinkBuilder.BuildResetPasswordLink(request.Token);

        await _passwordResetEmailSender.SendAsync(
            new PasswordResetEmailModel
            {
                Email = request.Email,
                DisplayName = request.DisplayName,
                UserName = request.UserName,
                ResetLink = resetLink
            },
            cancellationToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        await _resetPasswordValidator.ValidateAndThrowAsync(command, cancellationToken);

        var result = await _userAuthRepository.ResetPasswordAsync(
            command.Token,
            command.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw CreateValidationException("ResetPassword", result.ErrorMessage ?? "Password reset failed.");
        }
    }

    public Task<AuthenticatedUserModel?> GetCurrentUserAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult<AuthenticatedUserModel?>(null);
        }

        return _userAuthRepository.GetUserByAccessTokenAsync(accessToken, cancellationToken);
    }

    public async Task<AuthSessionResult?> RefreshAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _refreshSessionValidator.ValidateAndThrowAsync(command, cancellationToken);

        return await _userAuthRepository.RefreshAsync(command.RefreshToken, cancellationToken);
    }

    public async Task<AuthSessionResult?> SelectRoleAsync(
        string accessToken,
        SelectRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        await _selectRoleValidator.ValidateAndThrowAsync(command, cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        return await _userAuthRepository.SelectRoleAsync(accessToken, command.RoleId, cancellationToken);
    }

    public async Task<SsoLoginResult> SsoLoginAsync(
        SsoLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        await _ssoLoginValidator.ValidateAndThrowAsync(command, cancellationToken);

        var tokenValidation = await _ssoTokenProcessor.ValidateAsync(command.Token, cancellationToken);

        if (!tokenValidation.IsValid)
        {
            throw new UnauthorizedAccessException("The SSO token is invalid.");
        }

        if (tokenValidation.IsForbidden)
        {
            throw new AuthForbiddenException(tokenValidation.RedirectUrl);
        }

        var session = await _userAuthRepository.CreateSessionAsync(tokenValidation.Email, cancellationToken);

        if (session is null)
        {
            throw new AuthForbiddenException(tokenValidation.RedirectUrl);
        }

        return new SsoLoginResult
        {
            Success = true,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            ExpiresAt = session.ExpiresAt,
            User = session.User,
            Permissions = session.Permissions,
            RedirectUrl = tokenValidation.RedirectUrl
        };
    }

    private static ValidationException CreateValidationException(string propertyName, string errorMessage)
    {
        return new ValidationException(
            [new ValidationFailure(propertyName, errorMessage)]);
    }
}
