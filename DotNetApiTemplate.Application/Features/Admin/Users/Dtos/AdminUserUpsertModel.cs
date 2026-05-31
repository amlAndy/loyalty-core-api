namespace DotNetApiTemplate.Application.Features.Admin.Users.Dtos;

public sealed class AdminUserUpsertModel
{
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string[] RoleIds { get; init; } = [];
}
