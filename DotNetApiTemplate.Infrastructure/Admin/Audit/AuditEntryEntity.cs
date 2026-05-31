namespace DotNetApiTemplate.Infrastructure.Admin.Audit;

public sealed class AuditEntryEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? ActingUserId { get; set; }
    public string ActingUser { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public ICollection<AuditChangeDetailEntity> Details { get; set; } = [];
}
