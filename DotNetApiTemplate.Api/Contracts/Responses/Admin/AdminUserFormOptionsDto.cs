namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AdminUserFormOptionsDto
{
    public string[] Statuses { get; init; } = [];
    public AdminRoleOptionDto[] Roles { get; init; } = [];
}
