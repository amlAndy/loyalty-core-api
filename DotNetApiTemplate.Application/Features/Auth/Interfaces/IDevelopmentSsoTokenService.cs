namespace DotNetApiTemplate.Application.Features.Auth.Interfaces;

public interface IDevelopmentSsoTokenService
{
    Task<string?> GenerateTokenAsync(string email, CancellationToken cancellationToken = default);
}
