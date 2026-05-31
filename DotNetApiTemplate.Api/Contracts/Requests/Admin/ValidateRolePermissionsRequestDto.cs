namespace DotNetApiTemplate.Api.Contracts.Requests.Admin;

public sealed class ValidateRolePermissionsRequestDto
{
    public string? ParentRoleId { get; init; }
    public string[] Permissions { get; init; } = [];
    public string? CurrentRoleId { get; init; }
}
