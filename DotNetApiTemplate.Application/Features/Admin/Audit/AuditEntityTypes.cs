namespace DotNetApiTemplate.Application.Features.Admin.Audit;

public static class AuditEntityTypes
{
    public const string User = "User";
    public const string Role = "Role";
    public const string Review = "Review";

    public static readonly string[] Values = [User, Role, Review];
}
