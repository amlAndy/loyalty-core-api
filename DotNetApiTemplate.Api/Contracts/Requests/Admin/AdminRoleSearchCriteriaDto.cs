namespace DotNetApiTemplate.Api.Contracts.Requests.Admin;

public sealed class AdminRoleSearchCriteriaDto
{
    public string? Query { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}
