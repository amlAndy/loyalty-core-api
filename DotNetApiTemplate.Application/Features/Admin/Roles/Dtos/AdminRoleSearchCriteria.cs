namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public sealed class AdminRoleSearchCriteria
{
    public string? Query { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}
