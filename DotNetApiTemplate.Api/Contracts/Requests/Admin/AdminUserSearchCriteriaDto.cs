namespace DotNetApiTemplate.Api.Contracts.Requests.Admin;

public sealed class AdminUserSearchCriteriaDto
{
    public string? Query { get; init; }
    public string? RoleId { get; init; }
    public string? Status { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}
