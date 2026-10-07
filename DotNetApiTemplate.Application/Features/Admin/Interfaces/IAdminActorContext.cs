namespace DotNetApiTemplate.Application.Features.Admin.Interfaces;

public interface IAdminActorContext
{
    string? UserId { get; }
    string? ActiveRoleId { get; }
}
