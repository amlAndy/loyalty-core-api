using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.Auth.Exceptions;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class IdentityUserAuthRepository : IUserAuthRepository
{
    private const string ActiveStatus = "Active";
    private const string PermissionClaimType = "permission";
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public IdentityUserAuthRepository(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResult?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default,
        string? screen = null)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        var session = await CreateSessionForUserAsync(user, screen, cancellationToken);

        return new LoginResult
        {
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            ExpiresAt = session.ExpiresAt,
            User = session.User,
            Roles = session.User.Roles,
            Permissions = session.Permissions
        };
    }

    public async Task<bool> LogoutAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserFromAccessTokenAsync(accessToken);

        if (user is null)
        {
            return false;
        }

        var utcNow = DateTimeOffset.UtcNow;
        var activeRefreshTokens = await _dbContext.RefreshTokens
            .Where(refreshToken =>
                refreshToken.UserId == user.Id &&
                refreshToken.RevokedAtUtc == null &&
                refreshToken.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAtUtc = utcNow;
        }

        if (activeRefreshTokens.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<PasswordResetRequestResult?> CreatePasswordResetRequestAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(rawToken);

        _dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(PasswordResetTokenLifetime)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PasswordResetRequestResult
        {
            Email = user.Email ?? email,
            DisplayName = user.DisplayName,
            UserName = user.UserName,
            Token = rawToken
        };
    }

    public async Task<PasswordResetCompletionResult> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokenHash = HashToken(token);
        var storedToken = await _dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(passwordResetToken => passwordResetToken.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return FailedReset("Password reset link is invalid.");
        }

        if (storedToken.UsedAtUtc is not null)
        {
            return FailedReset("Password reset link has already been used.");
        }

        if (storedToken.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return FailedReset("Password reset link has expired.");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId);

        if (user is null)
        {
            return FailedReset("Password reset link is invalid.");
        }

        var identityResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityResetToken, newPassword);

        if (!result.Succeeded)
        {
            return FailedReset(result.Errors.FirstOrDefault()?.Description ?? "Password reset failed.");
        }

        storedToken.UsedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PasswordResetCompletionResult
        {
            Succeeded = true
        };
    }

    public async Task<AuthenticatedUserModel?> GetUserByAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var principal = _jwtTokenService.ValidateAccessToken(accessToken);
        var user = await GetUserFromPrincipalAsync(principal);

        if (user is null)
        {
            return null;
        }

        var roleData = await GetRoleDataAsync(user.Id, cancellationToken);
        var activeRoleId = ResolveActiveRoleId(roleData, principal?.FindFirstValue(JwtTokenService.ActiveRoleIdClaimType));

        return await MapUserAsync(user, roleData, activeRoleId, cancellationToken);
    }

    public async Task<AuthSessionResult?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var storedRefreshToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == refreshToken, cancellationToken);

        if (storedRefreshToken?.User is null ||
            storedRefreshToken.RevokedAtUtc is not null ||
            storedRefreshToken.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var user = storedRefreshToken.User;
        var roleData = await GetRoleDataAsync(user.Id, cancellationToken);
        var replacementRefreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);
        var activeRoleId = ResolveActiveRoleId(roleData, storedRefreshToken.ActiveRoleId);
        replacementRefreshToken.ActiveRoleId = activeRoleId;
        var (accessToken, accessTokenExpiresAt) = _jwtTokenService.GenerateAccessToken(
            user,
            roleData.RoleNames,
            replacementRefreshToken.Id,
            activeRoleId);

        storedRefreshToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        storedRefreshToken.ReplacedByToken = replacementRefreshToken.Token;
        _dbContext.RefreshTokens.Add(replacementRefreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult
        {
            AccessToken = accessToken,
            RefreshToken = replacementRefreshToken.Token,
            ExpiresAt = accessTokenExpiresAt,
            User = await MapUserAsync(user, roleData, activeRoleId, cancellationToken),
            Permissions = await GetPermissionsAsync(activeRoleId, cancellationToken)
        };
    }

    public async Task<AuthSessionResult?> SelectRoleAsync(
        string accessToken,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        var principal = _jwtTokenService.ValidateAccessToken(accessToken);
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionIdValue = principal?.FindFirstValue(JwtTokenService.SessionIdClaimType);

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(sessionIdValue, out var sessionId))
        {
            return null;
        }

        var storedRefreshToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(
                token => token.Id == sessionId && token.UserId == userId,
                cancellationToken);

        if (storedRefreshToken?.User is null ||
            storedRefreshToken.RevokedAtUtc is not null ||
            storedRefreshToken.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var roleData = await GetRoleDataAsync(userId, cancellationToken);

        if (!roleData.ContainsRole(roleId))
        {
            throw new AuthForbiddenException();
        }

        var replacementRefreshToken = _jwtTokenService.GenerateRefreshToken(userId);
        replacementRefreshToken.ActiveRoleId = roleId;

        storedRefreshToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        storedRefreshToken.ReplacedByToken = replacementRefreshToken.Token;
        _dbContext.RefreshTokens.Add(replacementRefreshToken);

        var (newAccessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(
            storedRefreshToken.User,
            roleData.RoleNames,
            replacementRefreshToken.Id,
            roleId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult
        {
            AccessToken = newAccessToken,
            RefreshToken = replacementRefreshToken.Token,
            ExpiresAt = expiresAt,
            User = await MapUserAsync(storedRefreshToken.User, roleData, roleId, cancellationToken),
            Permissions = await GetPermissionsAsync(roleId, cancellationToken)
        };
    }

    public async Task<AuthSessionResult?> CreateSessionAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        return await CreateSessionForUserAsync(user, null, cancellationToken);
    }

    private async Task<AuthSessionResult> CreateSessionForUserAsync(
        ApplicationUser user,
        string? loginScreen,
        CancellationToken cancellationToken)
    {
        var roleData = await GetRoleDataAsync(user.Id, cancellationToken);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);
        var activeRoleId = ResolveActiveRoleId(roleData, requestedActiveRoleId: null, loginScreen);
        refreshToken.ActiveRoleId = activeRoleId;
        var (accessToken, accessTokenExpiresAt) = _jwtTokenService.GenerateAccessToken(
            user,
            roleData.RoleNames,
            refreshToken.Id,
            activeRoleId);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = accessTokenExpiresAt,
            User = await MapUserAsync(user, roleData, activeRoleId, cancellationToken),
            Permissions = await GetPermissionsAsync(activeRoleId, cancellationToken)
        };
    }

    private Task<ApplicationUser?> GetUserFromAccessTokenAsync(string accessToken)
    {
        var principal = _jwtTokenService.ValidateAccessToken(accessToken);

        return GetUserFromPrincipalAsync(principal);
    }

    private Task<ApplicationUser?> GetUserFromPrincipalAsync(ClaimsPrincipal? principal)
    {
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        return string.IsNullOrWhiteSpace(userId)
            ? Task.FromResult<ApplicationUser?>(null)
            : _userManager.FindByIdAsync(userId);
    }

    private async Task<RoleData> GetRoleDataAsync(string userId, CancellationToken cancellationToken)
    {
        var roles = await (
            from userRole in _dbContext.UserRoles
            join role in _dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            orderby role.Name
            select new RoleProjection(role.Id, role.Name ?? string.Empty))
            .ToListAsync(cancellationToken);

        return new RoleData(
            roles.Select(role => role.Id).ToArray(),
            roles.Select(role => role.Name).ToArray());
    }

    private async Task<string[]> GetPermissionsAsync(string? activeRoleId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(activeRoleId))
        {
            return [];
        }

        return await _dbContext.RoleClaims
            .AsNoTracking()
            .Where(claim => claim.RoleId == activeRoleId && claim.ClaimType == PermissionClaimType)
            .Select(claim => claim.ClaimValue ?? string.Empty)
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct()
            .OrderBy(permission => permission)
            .ToArrayAsync(cancellationToken);
    }

    private static string? ResolveActiveRoleId(RoleData roleData, string? requestedActiveRoleId, string? loginScreen = null)
    {
        if (roleData.RoleIds.Length == 1)
        {
            return roleData.RoleIds[0];
        }

        if (roleData.ContainsRole(requestedActiveRoleId))
        {
            return requestedActiveRoleId;
        }

        var matchingRoleIds = string.IsNullOrWhiteSpace(loginScreen)
            ? []
            : roleData.RoleIds
                .Where((roleId, index) => IsRoleAllowedForLoginScreen(roleData.RoleNames[index], loginScreen))
                .ToArray();

        return matchingRoleIds.Length == 1 ? matchingRoleIds[0] : null;
    }

    private static bool IsRoleAllowedForLoginScreen(string roleName, string loginScreen)
    {
        return loginScreen.ToLowerInvariant() switch
        {
            LoginScreenTypes.Member => string.Equals(roleName, ApplicationRoleNames.Member, StringComparison.OrdinalIgnoreCase),
            LoginScreenTypes.Business => string.Equals(roleName, ApplicationRoleNames.BusinessOwner, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleName, ApplicationRoleNames.BusinessStaff, StringComparison.OrdinalIgnoreCase),
            LoginScreenTypes.Admin => string.Equals(roleName, ApplicationRoleNames.PlatformAdmin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleName, ApplicationRoleNames.TownAdmin, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private async Task<AuthenticatedUserModel> MapUserAsync(
        ApplicationUser user,
        RoleData roleData,
        string? activeRoleId,
        CancellationToken cancellationToken)
    {
        var businessId = await ResolveBusinessIdAsync(user.Id, roleData, activeRoleId, cancellationToken);

        return new AuthenticatedUserModel
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(user.Status) ? ActiveStatus : user.Status,
            RoleIds = roleData.RoleIds,
            Roles = roleData.RoleNames,
            ActiveRoleId = activeRoleId,
            BusinessId = businessId
        };
    }

    private async Task<string?> ResolveBusinessIdAsync(
        string userId,
        RoleData roleData,
        string? activeRoleId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(activeRoleId))
        {
            return null;
        }

        var activeRoleName = roleData.ResolveRoleName(activeRoleId);

        if (string.Equals(activeRoleName, ApplicationRoleNames.BusinessOwner, StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveOwnedBusinessIdAsync(userId, cancellationToken);
        }

        if (string.Equals(activeRoleName, ApplicationRoleNames.BusinessStaff, StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveStaffBusinessIdAsync(userId, cancellationToken);
        }

        return null;
    }

    private async Task<string?> ResolveOwnedBusinessIdAsync(string userId, CancellationToken cancellationToken)
    {
        var ownedBusinessIds = await _dbContext.BusinessProfiles
            .AsNoTracking()
            .Where(candidate => candidate.OwnerUserId == userId)
            .OrderBy(candidate => candidate.Id)
            .Select(candidate => candidate.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        return ownedBusinessIds.Count switch
        {
            0 => null,
            1 => ownedBusinessIds[0].ToString("D"),
            _ => throw new InvalidOperationException($"Ambiguous business owner context for user '{userId}'.")
        };
    }

    private async Task<string?> ResolveStaffBusinessIdAsync(string userId, CancellationToken cancellationToken)
    {
        var assignedBusinessIds = await _dbContext.BusinessStaffAccesses
            .AsNoTracking()
            .Where(candidate => candidate.UserId == userId && candidate.PinStatus == ActiveStatus)
            .Select(candidate => candidate.BusinessProfileId)
            .Distinct()
            .OrderBy(candidate => candidate)
            .Take(2)
            .ToListAsync(cancellationToken);

        return assignedBusinessIds.Count switch
        {
            0 => null,
            1 => assignedBusinessIds[0].ToString("D"),
            _ => throw new InvalidOperationException($"Ambiguous business staff context for user '{userId}'.")
        };
    }

    private static PasswordResetCompletionResult FailedReset(string message)
    {
        return new PasswordResetCompletionResult
        {
            Succeeded = false,
            ErrorMessage = message
        };
    }

    private static string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(tokenBytes));
    }

    private sealed record RoleProjection(string Id, string Name);

    private sealed record RoleData(string[] RoleIds, string[] RoleNames)
    {
        public bool ContainsRole(string? roleId)
        {
            return !string.IsNullOrWhiteSpace(roleId) && RoleIds.Contains(roleId, StringComparer.Ordinal);
        }

        public string? ResolveRoleName(string? roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return null;
            }

            var index = Array.FindIndex(RoleIds, currentRoleId => string.Equals(currentRoleId, roleId, StringComparison.Ordinal));
            return index >= 0 && index < RoleNames.Length ? RoleNames[index] : null;
        }
    };
}
