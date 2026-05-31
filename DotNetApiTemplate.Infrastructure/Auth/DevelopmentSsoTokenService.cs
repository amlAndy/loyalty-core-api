using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using DotNetApiTemplate.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class DevelopmentSsoTokenService : IDevelopmentSsoTokenService
{
    private static readonly HashSet<string> SeededUserEmails = new(
        DevelopmentIdentitySeeder.SeedUsers.Select(user => user.Email),
        StringComparer.OrdinalIgnoreCase);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public DevelopmentSsoTokenService(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<string?> GenerateTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!SeededUserEmails.Contains(email))
        {
            return null;
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        var token = _jwtTokenService.GenerateSsoToken(user);

        return token.Token;
    }
}
