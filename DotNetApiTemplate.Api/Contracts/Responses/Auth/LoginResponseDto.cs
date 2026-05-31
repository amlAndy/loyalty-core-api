namespace DotNetApiTemplate.Api.Contracts.Responses.Auth;

public sealed class LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string ExpiresAt { get; init; } = string.Empty;
    public AuthenticatedUserDto User { get; init; } = new();
    public string[] Roles { get; init; } = [];
    public string[] Permissions { get; init; } = [];
}
