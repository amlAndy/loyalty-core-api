using DotNetApiTemplate.Application.Features.Auth.Dtos;

namespace DotNetApiTemplate.Application.Features.Auth.Interfaces;

public interface ISsoTokenProcessor
{
    Task<SsoTokenValidationResult> ValidateAsync(string token, CancellationToken cancellationToken = default);
}
