namespace DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;

public sealed class AuditEntryModel
{
    public string Id { get; init; } = string.Empty;
    public string OccurredAt { get; init; } = string.Empty;
    public string ActingUser { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string ActionLabel { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public AuditChangeDetailModel[] Details { get; init; } = [];
}
