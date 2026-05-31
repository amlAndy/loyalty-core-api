using System.Net.Mime;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class SmtpEmailTransport : IEmailTransport
{
    private readonly IOptions<MailSettings> _mailSettings;
    private readonly ILogger<SmtpEmailTransport> _logger;

    public SmtpEmailTransport(
        IOptions<MailSettings> mailSettings,
        ILogger<SmtpEmailTransport> logger)
    {
        _mailSettings = mailSettings;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var settings = _mailSettings.Value;
        var endpoint = ParseSmtpEndpoint(settings.SmtpServer);
        var userName = RequireValue(settings.SmtpUserName, nameof(MailSettings.SmtpUserName));
        var password = RequireValue(settings.SmtpPassword, nameof(MailSettings.SmtpPassword));
        var fromAddress = RequireValue(settings.FromAddress, nameof(MailSettings.FromAddress));
        var fromName = settings.FromName?.Trim();

        using var mailMessage = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(fromName)
                ? new MailAddress(fromAddress)
                : new MailAddress(fromAddress, fromName),
            Subject = message.Subject,
            Body = string.IsNullOrWhiteSpace(message.HtmlBody) ? message.Body : message.HtmlBody,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
        };

        mailMessage.To.Add(message.To);

        if (!string.IsNullOrWhiteSpace(message.HtmlBody) && !string.IsNullOrWhiteSpace(message.Body))
        {
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.Body, null, MediaTypeNames.Text.Plain));
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, MediaTypeNames.Text.Html));
        }

        using var client = new SmtpClient(endpoint.Host, endpoint.Port)
        {
            EnableSsl = endpoint.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(userName, password)
        };

        _logger.LogInformation(
            "Sending password reset email via SMTP server {Host}:{Port} to {Recipient}.",
            endpoint.Host,
            endpoint.Port,
            message.To);

        await client.SendMailAsync(mailMessage, cancellationToken);
    }

    private static SmtpEndpoint ParseSmtpEndpoint(string? smtpServer)
    {
        var value = RequireValue(smtpServer, nameof(MailSettings.SmtpServer));

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"{nameof(MailSettings.SmtpServer)} must be an absolute URI.");
        }

        if (!uri.Scheme.Equals("smtp", StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals("smtps", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{nameof(MailSettings.SmtpServer)} must use smtp:// or smtps://.");
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException($"{nameof(MailSettings.SmtpServer)} must include a host.");
        }

        var isSecure = uri.Scheme.Equals("smtps", StringComparison.OrdinalIgnoreCase);
        var port = uri.IsDefaultPort
            ? (isSecure ? 465 : 25)
            : uri.Port;

        return new SmtpEndpoint(uri.Host, port, isSecure);
    }

    private static string RequireValue(string? value, string settingName)
    {
        var trimmedValue = value?.Trim();

        return string.IsNullOrWhiteSpace(trimmedValue)
            ? throw new InvalidOperationException($"{settingName} is not configured.")
            : trimmedValue;
    }

    private sealed record SmtpEndpoint(string Host, int Port, bool EnableSsl);
}
