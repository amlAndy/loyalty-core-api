namespace DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;

public sealed class AuditSearchCriteria
{
    public string? SearchText { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public string? ActingUser { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}
