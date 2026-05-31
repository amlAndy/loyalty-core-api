namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public static class ApplicationRoleNames
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string TownAdmin = "TownAdmin";
    public const string BusinessOwner = "BusinessOwner";
    public const string BusinessStaff = "BusinessStaff";
    public const string Member = "Member";
    public const string SupportUser = "SupportUser";

    public static readonly string[] AdminRoles =
    [
        PlatformAdmin,
        TownAdmin
    ];
}
