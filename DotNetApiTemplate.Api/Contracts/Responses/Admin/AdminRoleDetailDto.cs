namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AdminRoleDetailDto : AdminRoleSummaryDto
{
    public AdminRoleChildDto[] ChildRoles { get; init; } = [];
}
