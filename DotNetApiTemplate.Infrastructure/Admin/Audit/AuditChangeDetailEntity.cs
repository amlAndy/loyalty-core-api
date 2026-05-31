namespace DotNetApiTemplate.Infrastructure.Admin.Audit;

public sealed class AuditChangeDetailEntity
{
    public Guid Id { get; set; }
    public Guid AuditEntryId { get; set; }
    public int Sequence { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? BeforeValueJson { get; set; }
    public string? AfterValueJson { get; set; }
    public AuditEntryEntity AuditEntry { get; set; } = null!;
}
