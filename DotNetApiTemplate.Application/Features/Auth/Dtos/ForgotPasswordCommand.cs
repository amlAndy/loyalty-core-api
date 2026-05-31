namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class ForgotPasswordCommand
{
    public string Email { get; init; } = string.Empty;
}
