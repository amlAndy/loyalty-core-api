namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class SsoLoginResult
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public AuthenticatedUserModel? User { get; init; }
    public string[] Permissions { get; init; } = [];
    public string? RedirectUrl { get; init; }
}
