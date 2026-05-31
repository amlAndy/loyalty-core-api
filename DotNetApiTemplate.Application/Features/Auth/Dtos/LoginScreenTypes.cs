namespace DotNetApiTemplate.Application.Features.Auth.Dtos;

public static class LoginScreenTypes
{
    public const string Member = "member";
    public const string Business = "business";
    public const string Admin = "admin";

    public static readonly string[] Values =
    [
        Member,
        Business,
        Admin
    ];
}
