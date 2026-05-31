namespace DotNetApiTemplate.Application.Features.Auth.Interfaces;

public interface IPasswordResetLinkBuilder
{
    string BuildResetPasswordLink(string token);
}
