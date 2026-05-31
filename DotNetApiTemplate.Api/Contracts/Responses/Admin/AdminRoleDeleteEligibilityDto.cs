namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AdminRoleDeleteEligibilityDto
{
    public bool CanDelete { get; init; }
    public string? Reason { get; init; }
}
