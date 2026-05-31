using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.Auth.Exceptions;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using DotNetApiTemplate.Application.Features.Auth.Services;
using DotNetApiTemplate.Application.Features.Auth.Validation;
using FluentValidation;
using Xunit;

namespace DotNetApiTemplate.Application.Tests.Features.Auth.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_returns_login_result_for_valid_credentials()
    {
        var repository = new FakeUserAuthRepository
        {
            LoginResult = CreateLoginResult()
        };

        var service = CreateService(repository: repository);

        var result = await service.LoginAsync(new LoginCommand
        {
            Email = "user@example.com",
            Password = "password"
        });

        Assert.NotNull(result);
        Assert.Equal("user@example.com", repository.LastAuthenticatedEmail);
        Assert.Equal("access-token", result!.AccessToken);
        Assert.Equal(["read:profile"], result.Permissions);
        Assert.Equal("role-1", result.User.ActiveRoleId);
    }

    [Fact]
    public async Task ForgotPasswordAsync_does_not_fail_for_unknown_email()
    {
        var repository = new FakeUserAuthRepository();
        var service = CreateService(repository: repository);

        await service.ForgotPasswordAsync(new ForgotPasswordCommand
        {
            Email = "missing@example.com"
        });

        Assert.Equal("missing@example.com", repository.LastForgotPasswordEmail);
    }

    [Fact]
    public async Task ResetPasswordAsync_throws_validation_exception_when_reset_fails()
    {
        var service = CreateService(repository: new FakeUserAuthRepository
        {
            ResetPasswordResult = new PasswordResetCompletionResult
            {
                Succeeded = false,
                ErrorMessage = "Password reset link is invalid."
            }
        });

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ResetPasswordAsync(new ResetPasswordCommand
            {
                Token = "invalid-token",
                NewPassword = "new-password",
                ConfirmPassword = "new-password"
            }));

        Assert.Contains(exception.Errors, error => error.PropertyName == "ResetPassword");
    }

    [Fact]
    public async Task RefreshAsync_returns_auth_session_for_valid_refresh_token()
    {
        var repository = new FakeUserAuthRepository
        {
            RefreshResult = CreateSessionResult()
        };

        var service = CreateService(repository: repository);

        var result = await service.RefreshAsync(new RefreshSessionCommand
        {
            RefreshToken = "refresh-token"
        });

        Assert.NotNull(result);
        Assert.Equal("refresh-token", repository.LastRefreshToken);
        Assert.Equal("session-access-token", result!.AccessToken);
        Assert.Equal("role-2", result.User.ActiveRoleId);
    }

    [Fact]
    public async Task SelectRoleAsync_returns_updated_session_for_valid_role()
    {
        var repository = new FakeUserAuthRepository
        {
            SelectRoleResult = CreateSessionResult()
        };
        var service = CreateService(repository: repository);

        var result = await service.SelectRoleAsync(
            "access-token",
            new SelectRoleCommand
            {
                RoleId = "role-2"
            });

        Assert.NotNull(result);
        Assert.Equal("access-token", repository.LastSelectRoleAccessToken);
        Assert.Equal("role-2", repository.LastSelectedRoleId);
    }

    [Fact]
    public async Task SsoLoginAsync_returns_session_when_sso_token_is_valid()
    {
        var service = CreateService(
            repository: new FakeUserAuthRepository
            {
                CreatedSession = CreateSessionResult()
            },
            ssoTokenProcessor: new FakeSsoTokenProcessor
            {
                ValidationResult = new SsoTokenValidationResult
                {
                    IsValid = true,
                    Email = "user@example.com"
                }
            });

        var result = await service.SsoLoginAsync(new SsoLoginCommand
        {
            Token = "sso-token"
        });

        Assert.True(result.Success);
        Assert.Equal("session-access-token", result.AccessToken);
        Assert.Equal("user@example.com", result.User!.Email);
        Assert.Equal(["write:orders"], result.Permissions);
    }

    [Fact]
    public async Task SsoLoginAsync_throws_forbidden_when_sso_token_is_forbidden()
    {
        var service = CreateService(
            ssoTokenProcessor: new FakeSsoTokenProcessor
            {
                ValidationResult = new SsoTokenValidationResult
                {
                    IsValid = true,
                    IsForbidden = true,
                    RedirectUrl = "/signin"
                }
            });

        var exception = await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            service.SsoLoginAsync(new SsoLoginCommand
            {
                Token = "sso-token"
            }));

        Assert.Equal("/signin", exception.RedirectUrl);
    }

    private static AuthService CreateService(
        IUserAuthRepository? repository = null,
        ISsoTokenProcessor? ssoTokenProcessor = null)
    {
        return new AuthService(
            new LoginCommandValidator(),
            new ForgotPasswordCommandValidator(),
            new ResetPasswordCommandValidator(),
            new RefreshSessionCommandValidator(),
            new SelectRoleCommandValidator(),
            new SsoLoginCommandValidator(),
            repository ?? new FakeUserAuthRepository(),
            new FakePasswordResetLinkBuilder(),
            new FakePasswordResetEmailSender(),
            ssoTokenProcessor ?? new FakeSsoTokenProcessor());
    }

    private static LoginResult CreateLoginResult()
    {
        return new LoginResult
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            User = CreateUser("role-1", ["Admin"]),
            Roles = ["Admin"],
            Permissions = ["read:profile"]
        };
    }

    private static AuthSessionResult CreateSessionResult()
    {
        return new AuthSessionResult
        {
            AccessToken = "session-access-token",
            RefreshToken = "session-refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            User = CreateUser("role-2", ["Operator", "Reviewer"]),
            Permissions = ["write:orders"]
        };
    }

    private static AuthenticatedUserModel CreateUser(string? activeRoleId, string[] roles)
    {
        return new AuthenticatedUserModel
        {
            Id = "user-1",
            DisplayName = "Test User",
            Email = "user@example.com",
            Status = "Active",
            RoleIds = roles.Select((_, index) => $"role-{index + 1}").ToArray(),
            Roles = roles,
            ActiveRoleId = activeRoleId
        };
    }

    private sealed class FakeUserAuthRepository : IUserAuthRepository
    {
        public LoginResult? LoginResult { get; init; }
        public PasswordResetCompletionResult ResetPasswordResult { get; init; } = new() { Succeeded = true };
        public AuthSessionResult? RefreshResult { get; init; }
        public AuthSessionResult? SelectRoleResult { get; init; }
        public AuthSessionResult? CreatedSession { get; init; }
        public string? LastAuthenticatedEmail { get; private set; }
        public string? LastForgotPasswordEmail { get; private set; }
        public string? LastRefreshToken { get; private set; }
        public string? LastSelectRoleAccessToken { get; private set; }
        public string? LastSelectedRoleId { get; private set; }

        public Task<LoginResult?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            LastAuthenticatedEmail = email;
            return Task.FromResult(LoginResult);
        }

        public Task<bool> LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<PasswordResetRequestResult?> CreatePasswordResetRequestAsync(string email, CancellationToken cancellationToken = default)
        {
            LastForgotPasswordEmail = email;
            return Task.FromResult<PasswordResetRequestResult?>(null);
        }

        public Task<PasswordResetCompletionResult> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ResetPasswordResult);
        }

        public Task<AuthenticatedUserModel?> GetUserByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthenticatedUserModel?>(CreateUser("role-1", ["Admin"]));
        }

        public Task<AuthSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            LastRefreshToken = refreshToken;
            return Task.FromResult(RefreshResult);
        }

        public Task<AuthSessionResult?> SelectRoleAsync(string accessToken, string roleId, CancellationToken cancellationToken = default)
        {
            LastSelectRoleAccessToken = accessToken;
            LastSelectedRoleId = roleId;
            return Task.FromResult(SelectRoleResult);
        }

        public Task<AuthSessionResult?> CreateSessionAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreatedSession);
        }
    }

    private sealed class FakePasswordResetLinkBuilder : IPasswordResetLinkBuilder
    {
        public string BuildResetPasswordLink(string token)
        {
            return $"https://example.test/reset-password?token={token}";
        }
    }

    private sealed class FakePasswordResetEmailSender : IPasswordResetEmailSender
    {
        public Task SendAsync(PasswordResetEmailModel model, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSsoTokenProcessor : ISsoTokenProcessor
    {
        public SsoTokenValidationResult ValidationResult { get; init; } = new();

        public Task<SsoTokenValidationResult> ValidateAsync(string token, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ValidationResult);
        }
    }
}
