namespace DotNetApiTemplate.Api.Contracts.Requests.Auth;

public sealed class RefreshSessionRequestDto
{
    public string RefreshToken { get; init; } = string.Empty;
}
