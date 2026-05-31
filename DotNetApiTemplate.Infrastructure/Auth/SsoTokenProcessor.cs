using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class SsoTokenProcessor : ISsoTokenProcessor
{
    private readonly JwtTokenService _jwtTokenService;

    public SsoTokenProcessor(JwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public Task<SsoTokenValidationResult> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(new SsoTokenValidationResult());
        }

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                token,
                _jwtTokenService.CreateTokenValidationParameters(validateLifetime: true),
                out _);

            var email = principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? principal.FindFirstValue("upn")
                ?? principal.FindFirstValue("preferred_username")
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                return Task.FromResult(new SsoTokenValidationResult());
            }

            return Task.FromResult(new SsoTokenValidationResult
            {
                IsValid = true,
                Email = email,
                RedirectUrl = principal.FindFirstValue("redirectUrl")
            });
        }
        catch (SecurityTokenException)
        {
            return Task.FromResult(new SsoTokenValidationResult());
        }
        catch (ArgumentException)
        {
            return Task.FromResult(new SsoTokenValidationResult());
        }
    }
}
