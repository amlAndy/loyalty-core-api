namespace DotNetApiTemplate.Application.Features.Admin.Users.Dtos;

public sealed class AdminUserSearchResult
{
    public AdminUserSummaryModel[] Items { get; init; } = [];
    public int Total { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
