namespace DotNetApiTemplate.Api.Contracts.Requests.Admin;

public sealed class AdminUserUpsertDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string[] RoleIds { get; init; } = [];
}
