namespace DotNetApiTemplate.Api.Contracts.Requests.Admin;

public sealed class UpdateAdminRoleRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ParentRoleId { get; init; }
    public string[] Permissions { get; init; } = [];
}
