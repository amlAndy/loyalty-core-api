namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class PasswordResetCompletionResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
}
