namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public sealed class AdminRoleDeleteEligibilityModel
{
    public bool CanDelete { get; init; }
    public string? Reason { get; init; }
}
