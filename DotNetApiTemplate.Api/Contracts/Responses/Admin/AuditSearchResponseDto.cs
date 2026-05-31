namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AuditSearchResponseDto
{
    public AuditEntryDto[] Items { get; init; } = [];
    public int Total { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
