namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class SsoTokenValidationResult
{
    public bool IsValid { get; init; }
    public bool IsForbidden { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? RedirectUrl { get; init; }
}
