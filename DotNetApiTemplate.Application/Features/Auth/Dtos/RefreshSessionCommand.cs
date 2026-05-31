namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class RefreshSessionCommand
{
    public string RefreshToken { get; init; } = string.Empty;
}
