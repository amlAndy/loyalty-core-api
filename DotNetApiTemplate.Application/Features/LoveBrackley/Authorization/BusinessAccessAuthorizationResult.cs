namespace DotNetApiTemplate.Application.Features.LoveBrackley.Authorization;

public sealed record BusinessAccessAuthorizationResult(BusinessAccessDecision Decision)
{
    public static BusinessAccessAuthorizationResult Allowed { get; } = new(BusinessAccessDecision.Allowed);
    public static BusinessAccessAuthorizationResult Forbidden { get; } = new(BusinessAccessDecision.Forbidden);
    public static BusinessAccessAuthorizationResult NotFound { get; } = new(BusinessAccessDecision.NotFound);

    public bool IsAllowed => Decision == BusinessAccessDecision.Allowed;
}
