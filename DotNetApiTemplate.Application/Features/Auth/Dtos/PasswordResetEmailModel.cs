namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class PasswordResetEmailModel
{
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserName { get; init; }
    public string ResetLink { get; init; } = string.Empty;
}
