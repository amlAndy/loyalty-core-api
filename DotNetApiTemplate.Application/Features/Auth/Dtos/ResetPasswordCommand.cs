namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class ResetPasswordCommand
{
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}
