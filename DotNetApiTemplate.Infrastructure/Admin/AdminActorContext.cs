using System.Security.Claims;
using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace DotNetApiTemplate.Infrastructure.Admin;

public sealed class AdminActorContext : IAdminActorContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminActorContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

    public string? ActiveRoleId => _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtTokenService.ActiveRoleIdClaimType);
}
