namespace DotNetApiTemplate.Api.Contracts.Requests.Auth;

public sealed class ForgotPasswordRequestDto
{
    public string Email { get; init; } = string.Empty;
}
