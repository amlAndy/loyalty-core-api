using DotNetApiTemplate.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;

namespace DotNetApiTemplate.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
