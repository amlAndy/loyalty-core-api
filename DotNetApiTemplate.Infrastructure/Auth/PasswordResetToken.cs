using DotNetApiTemplate.Infrastructure.Identity;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class PasswordResetToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public ApplicationUser? User { get; set; }
}
