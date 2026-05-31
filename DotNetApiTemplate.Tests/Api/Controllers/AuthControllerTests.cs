using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests.Auth;
using DotNetApiTemplate.Api.Contracts.Responses.Auth;
using DotNetApiTemplate.Api.Controllers;
using DotNetApiTemplate.Api.Mapping;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetApiTemplate.Tests.Api.Controllers;

public sealed class AuthControllerTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(configuration =>
    {
        configuration.AddProfile<AuthMappingProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task ResetPassword_WhenValidationFails_ReturnsSimpleMessagePayload()
    {
        var controller = new AuthController(
            Mapper,
            new ValidationThrowingAuthService(new ValidationException(
                [new ValidationFailure("ResetPassword", "Password reset link has expired.")])));

        var result = await controller.ResetPassword(
            new ResetPasswordRequestDto
            {
                Token = "expired-token",
                NewPassword = "Password1!",
                ConfirmPassword = "Password1!"
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<AuthErrorResponseDto>(badRequest.Value);

        Assert.Equal("Password reset link has expired.", payload.Message);
    }

    private sealed class ValidationThrowingAuthService : IAuthService
    {
        private readonly ValidationException _exception;

        public ValidationThrowingAuthService(ValidationException exception)
        {
            _exception = exception;
        }

        public Task<LoginResult?> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ForgotPasswordAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
            => Task.FromException(_exception);

        public Task<AuthenticatedUserModel?> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AuthSessionResult?> RefreshAsync(RefreshSessionCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SsoLoginResult> SsoLoginAsync(SsoLoginCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
