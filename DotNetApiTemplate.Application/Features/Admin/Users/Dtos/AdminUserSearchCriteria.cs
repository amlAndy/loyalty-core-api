namespace DotNetApiTemplate.Application.Features.Admin.Users.Dtos;

public sealed class AdminUserSearchCriteria
{
    public string? Query { get; init; }
    public string? RoleId { get; init; }
    public string? Status { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}
