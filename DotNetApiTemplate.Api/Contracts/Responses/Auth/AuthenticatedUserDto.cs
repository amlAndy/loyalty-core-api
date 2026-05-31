namespace DotNetApiTemplate.Api.Contracts.Responses.Auth;

public sealed class AuthenticatedUserDto
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string[] RoleIds { get; init; } = [];
    public string[] Roles { get; init; } = [];
    public string? ActiveRoleId { get; init; }
}
