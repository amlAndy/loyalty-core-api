namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public sealed class AdminRoleSearchResult
{
    public AdminRoleSummaryModel[] Items { get; init; } = [];
    public int Total { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
