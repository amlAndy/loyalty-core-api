namespace DotNetApiTemplate.Application.Features.LoveBrackley.Authorization;

public interface IBusinessAccessAuthorizationService
{
    Task<BusinessAccessAuthorizationResult> AuthorizeAsync(
        string? userId,
        string? activeRoleId,
        Guid businessId,
        BusinessAccessOperation operation,
        CancellationToken cancellationToken = default);
}
