namespace DotNetApiTemplate.Api.Contracts.Responses.Auth;

public sealed class AuthSessionDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string ExpiresAt { get; init; } = string.Empty;
    public AuthenticatedUserDto User { get; init; } = new();
    public string[] Permissions { get; init; } = [];
}
