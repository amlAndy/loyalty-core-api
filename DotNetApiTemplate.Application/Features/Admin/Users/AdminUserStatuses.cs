namespace DotNetApiTemplate.Application.Features.Admin.Users;

public static class AdminUserStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Locked = "Locked";
    public const string PendingInvite = "PendingInvite";

    public static readonly string[] Values = [Active, Inactive, Locked, PendingInvite];
}
