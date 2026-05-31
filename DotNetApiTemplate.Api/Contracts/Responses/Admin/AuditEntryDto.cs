namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AuditEntryDto
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
    public AuditChangeDetailDto[] Details { get; init; } = [];
}
