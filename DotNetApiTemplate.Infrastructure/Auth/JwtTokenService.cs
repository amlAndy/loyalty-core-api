using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DotNetApiTemplate.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class JwtTokenService
{
    public const string SessionIdClaimType = JwtRegisteredClaimNames.Sid;
    public const string ActiveRoleIdClaimType = "active_role_id";

    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secret;
    private readonly int _accessTokenLifetimeMinutes;
    private readonly int _refreshTokenLifetimeDays;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        _secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _accessTokenLifetimeMinutes = ReadPositiveInt(configuration["Jwt:AccessTokenLifetimeMinutes"], 60);
        _refreshTokenLifetimeDays = ReadPositiveInt(configuration["Jwt:RefreshTokenLifetimeDays"], 7);
    }

    public (string Token, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(
        ApplicationUser user,
        IReadOnlyCollection<string> roles,
        Guid sessionId,
        string? activeRoleId)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(_accessTokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(SessionIdClaimType, sessionId.ToString("D")),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(activeRoleId))
        {
            claims.Add(new Claim(ActiveRoleIdClaimType, activeRoleId));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(GetSecurityKey(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public (string Token, DateTimeOffset ExpiresAtUtc) GenerateSsoToken(ApplicationUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(_accessTokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var credentials = new SigningCredentials(GetSecurityKey(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(
                token,
                CreateTokenValidationParameters(validateLifetime: true),
                out _);
        }
        catch
        {
            return null;
        }
    }

    public TokenValidationParameters CreateTokenValidationParameters(bool validateLifetime)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSecurityKey(),
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    }

    public RefreshToken GenerateRefreshToken(string userId, string? createdByIp = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = GenerateSecureToken(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_refreshTokenLifetimeDays),
            CreatedByIp = createdByIp
        };
    }

    private SymmetricSecurityKey GetSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
    }

    private static int ReadPositiveInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);

        return token.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
