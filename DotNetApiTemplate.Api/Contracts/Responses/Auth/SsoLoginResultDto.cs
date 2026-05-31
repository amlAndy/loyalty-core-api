namespace DotNetApiTemplate.Api.Contracts.Responses.Auth;

public sealed class SsoLoginResultDto
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? ExpiresAt { get; init; }
    public AuthenticatedUserDto? User { get; init; }
    public string[] Permissions { get; init; } = [];
    public string? RedirectUrl { get; init; }
}
