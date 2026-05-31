namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public sealed class AdminRoleDetailModel : AdminRoleSummaryModel
{
    public AdminRoleChildModel[] ChildRoles { get; init; } = [];
}
