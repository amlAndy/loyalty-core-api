namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AdminUserSearchResponseDto
{
    public AdminUserSummaryDto[] Items { get; init; } = [];
    public int Total { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
