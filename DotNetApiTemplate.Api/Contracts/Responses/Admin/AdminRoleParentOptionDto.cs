namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AdminRoleParentOptionDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ParentRoleId { get; init; }
    public bool IsRootRole { get; init; }
    public bool IsSystemRole { get; init; }
}
