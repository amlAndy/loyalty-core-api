using System.Globalization;
using System.Net;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class PasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly IOptions<MailSettings> _mailSettings;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IEmailTransport _emailTransport;
    private readonly ILogger<PasswordResetEmailSender> _logger;

    public PasswordResetEmailSender(
        IOptions<MailSettings> mailSettings,
        IHostEnvironment hostEnvironment,
        IEmailTransport emailTransport,
        ILogger<PasswordResetEmailSender> logger)
    {
        _mailSettings = mailSettings;
        _hostEnvironment = hostEnvironment;
        _emailTransport = emailTransport;
        _logger = logger;
    }

    public async Task SendAsync(PasswordResetEmailModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var settings = _mailSettings.Value;
        var intendedRecipient = model.Email.Trim();
        var actualRecipient = ResolveRecipient(settings, intendedRecipient);
        var firstNameOrGreeting = ResolveFirstNameOrGreeting(model.DisplayName, model.UserName, model.Email);
        var subject = settings.PasswordResetSubject?.Trim();
        var bodyTemplate = settings.PasswordResetBodyTemplate;

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("MailSettings:PasswordResetSubject is not configured.");
        }

        if (string.IsNullOrWhiteSpace(bodyTemplate))
        {
            throw new InvalidOperationException("MailSettings:PasswordResetBodyTemplate is not configured.");
        }

        var body = string.Format(
            CultureInfo.InvariantCulture,
            bodyTemplate,
            firstNameOrGreeting,
            model.ResetLink,
            settings.PasswordResetApplicationName,
            settings.PasswordResetSupportEmail,
            settings.PrimaryColor,
            settings.PasswordResetSupportTeamName);
        var htmlBody = BuildHtmlBody(settings, firstNameOrGreeting, model.ResetLink);

        _logger.LogInformation(
            "Dispatching password reset email. Intended recipient: {IntendedRecipient}. Actual recipient: {ActualRecipient}.",
            intendedRecipient,
            actualRecipient);

        await _emailTransport.SendAsync(
            new EmailMessage
            {
                To = actualRecipient,
                Subject = subject,
                Body = body,
                HtmlBody = htmlBody
            },
            cancellationToken);
    }

    private static string BuildHtmlBody(MailSettings settings, string firstNameOrGreeting, string resetLink)
    {
        var applicationName = EncodeOrFallback(settings.PasswordResetApplicationName, "your account");
        var supportEmailValue = string.IsNullOrWhiteSpace(settings.PasswordResetSupportEmail)
            ? settings.FromAddress
            : settings.PasswordResetSupportEmail;
        var supportEmail = EncodeOrFallback(supportEmailValue, "support@example.com");
        var supportEmailHref = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(supportEmailValue) ? "support@example.com" : supportEmailValue.Trim());
        var supportTeamName = EncodeOrFallback(settings.PasswordResetSupportTeamName, "Support Team");
        var primaryColor = EncodeOrFallback(settings.PrimaryColor, "#4A2683");
        var greeting = WebUtility.HtmlEncode(firstNameOrGreeting);
        var encodedResetLink = WebUtility.HtmlEncode(resetLink);

        return $"""
<!DOCTYPE html>
<html lang="en">
<body style="margin:0;padding:24px;background-color:#ffffff;font-family:Arial,Helvetica,sans-serif;color:#1f2933;font-size:15px;line-height:1.6;">
    <div style="max-width:620px;margin:0 auto;padding:28px;background-color:#ffffff;border:1px solid #ddd6e8;border-radius:10px;border-top:6px solid {primaryColor};box-sizing:border-box;">
        <p style="margin:0 0 16px 0;">Hello {greeting},</p>
        <p style="margin:0 0 16px 0;">We received a request to reset the password for your <strong>{applicationName}</strong> account.</p>
        <p style="margin:0 0 18px 0;">Please follow the link below to reset your password:</p>
        <p style="margin:0 0 26px 0;">
            <a href="{encodedResetLink}" style="display:inline-block;padding:11px 20px;background-color:#ffffff;color:{primaryColor};border:2px solid {primaryColor};border-radius:6px;text-decoration:none;font-weight:700;font-size:14px;">Reset my password</a>
        </p>
        <p style="margin:0 0 16px 0;">For security purposes, this link will expire in <strong>1 hour</strong>.</p>
        <p style="margin:0 0 16px 0;">If you did not request a new password, please ignore this e-mail. Nothing will change unless you use the link above.</p>
        <p style="margin:0 0 16px 0;">If you have any issues, please contact <a href="mailto:{supportEmailHref}" style="color:{primaryColor};text-decoration:underline;">{supportEmail}</a> and we will deal with it as quickly as possible.</p>
        <p style="margin:28px 0 0 0;">Thanks,<br><br><strong>{supportTeamName}</strong></p>
    </div>
</body>
</html>
""";
    }

    internal static string ResolveFirstNameOrGreeting(string? displayName, string? userName, string? email)
    {
        var firstName = FirstToken(displayName)
            ?? FirstToken(userName)
            ?? EmailLocalPart(email);

        return string.IsNullOrWhiteSpace(firstName)
            ? "Hello"
            : firstName;
    }

    private string ResolveRecipient(MailSettings settings, string intendedRecipient)
    {
        var overrideRecipient = settings.DevelopmentOverrideRecipient?.Trim();

        return _hostEnvironment.IsDevelopment() && !string.IsNullOrWhiteSpace(overrideRecipient)
            ? overrideRecipient
            : intendedRecipient;
    }

    private static string? FirstToken(string? value)
    {
        var trimmedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            return null;
        }

        var token = trimmedValue
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(token)
            ? null
            : token;
    }

    private static string? EmailLocalPart(string? email)
    {
        var trimmedEmail = email?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedEmail))
        {
            return null;
        }

        var delimiterIndex = trimmedEmail.IndexOf('@');

        if (delimiterIndex <= 0)
        {
            return trimmedEmail;
        }

        return trimmedEmail[..delimiterIndex].Trim();
    }

    private static string EncodeOrFallback(string? value, string fallback)
    {
        var resolvedValue = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();

        return WebUtility.HtmlEncode(resolvedValue);
    }
}
