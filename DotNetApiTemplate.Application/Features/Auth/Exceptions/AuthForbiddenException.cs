namespace DotNetApiTemplate.Application.Features.Auth.Exceptions;

public sealed class AuthForbiddenException : Exception
{
    public AuthForbiddenException(string? redirectUrl = null)
        : base("The request is forbidden.")
    {
        RedirectUrl = redirectUrl;
    }

    public string? RedirectUrl { get; }
}
