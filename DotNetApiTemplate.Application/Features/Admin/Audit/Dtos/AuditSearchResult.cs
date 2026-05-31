namespace DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;

public sealed class AuditSearchResult
{
    public AuditEntryModel[] Items { get; init; } = [];
    public int Total { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
