using Microsoft.AspNetCore.Identity;

namespace DotNetApiTemplate.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;
    public string? ParentRoleId { get; set; }
    public bool IsRootRole { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationRole? ParentRole { get; set; }
    public ICollection<ApplicationRole> ChildRoles { get; set; } = [];
}
