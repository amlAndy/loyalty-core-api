using DotNetApiTemplate.Application.Features.Auth.Dtos;

namespace DotNetApiTemplate.Application.Features.Auth.Interfaces;

public interface IUserAuthRepository
{
    Task<LoginResult?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<PasswordResetRequestResult?> CreatePasswordResetRequestAsync(string email, CancellationToken cancellationToken = default);
    Task<PasswordResetCompletionResult> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    Task<AuthenticatedUserModel?> GetUserByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<AuthSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthSessionResult?> SelectRoleAsync(string accessToken, string roleId, CancellationToken cancellationToken = default);
    Task<AuthSessionResult?> CreateSessionAsync(string email, CancellationToken cancellationToken = default);
}
