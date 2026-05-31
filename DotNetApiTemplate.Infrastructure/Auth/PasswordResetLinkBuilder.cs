using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using Microsoft.Extensions.Options;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class PasswordResetLinkBuilder : IPasswordResetLinkBuilder
{
    private readonly IOptions<MailSettings> _mailSettings;

    public PasswordResetLinkBuilder(IOptions<MailSettings> mailSettings)
    {
        _mailSettings = mailSettings;
    }

    public string BuildResetPasswordLink(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("A password reset token is required.", nameof(token));
        }

        var frontendBaseUrl = _mailSettings.Value.FrontendBaseUrl?.Trim();

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new InvalidOperationException("MailSettings:FrontendBaseUrl is not configured.");
        }

        return $"{frontendBaseUrl.TrimEnd('/')}/auth/reset-password?token={Uri.EscapeDataString(token)}";
    }
}
