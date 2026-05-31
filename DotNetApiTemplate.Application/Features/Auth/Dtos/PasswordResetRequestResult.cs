namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class PasswordResetRequestResult
{
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserName { get; init; }
    public string Token { get; init; } = string.Empty;
}
