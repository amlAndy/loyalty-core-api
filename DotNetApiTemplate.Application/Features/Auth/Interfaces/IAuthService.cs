using DotNetApiTemplate.Application.Features.Auth.Dtos;

namespace DotNetApiTemplate.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(string accessToken, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default);
    Task<AuthenticatedUserModel?> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<AuthSessionResult?> RefreshAsync(RefreshSessionCommand command, CancellationToken cancellationToken = default);
    Task<AuthSessionResult?> SelectRoleAsync(string accessToken, SelectRoleCommand command, CancellationToken cancellationToken = default);
    Task<SsoLoginResult> SsoLoginAsync(SsoLoginCommand command, CancellationToken cancellationToken = default);
}
