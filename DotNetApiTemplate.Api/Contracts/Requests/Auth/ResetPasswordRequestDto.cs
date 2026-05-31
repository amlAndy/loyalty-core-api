namespace DotNetApiTemplate.Api.Contracts.Requests.Auth;

public sealed class ResetPasswordRequestDto
{
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}
