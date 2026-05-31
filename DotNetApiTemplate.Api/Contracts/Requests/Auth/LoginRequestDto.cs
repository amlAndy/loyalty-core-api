namespace DotNetApiTemplate.Api.Contracts.Requests.Auth;

public sealed class LoginRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Screen { get; init; } = string.Empty;
}
