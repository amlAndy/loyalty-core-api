namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class LoginResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public AuthenticatedUserModel User { get; init; } = new();
    public string[] Roles { get; init; } = [];
    public string[] Permissions { get; init; } = [];
}
