namespace DotNetApiTemplate.Application.Features.Admin.Users.Dtos;

public sealed class AdminUserFormOptionsModel
{
    public string[] Statuses { get; init; } = [];
    public AdminRoleOptionModel[] Roles { get; init; } = [];
}
