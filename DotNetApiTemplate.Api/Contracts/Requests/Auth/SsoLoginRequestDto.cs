namespace DotNetApiTemplate.Api.Contracts.Requests.Auth;

public sealed class SsoLoginRequestDto
{
    public string Token { get; init; } = string.Empty;
}
