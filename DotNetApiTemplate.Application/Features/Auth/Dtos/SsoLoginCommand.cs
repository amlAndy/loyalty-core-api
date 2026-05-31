namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public sealed class SsoLoginCommand
{
    public string Token { get; init; } = string.Empty;
}
