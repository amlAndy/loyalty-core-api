using Microsoft.Extensions.Logging;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class LoggingEmailTransport : IEmailTransport
{
    private readonly ILogger<LoggingEmailTransport> _logger;

    public LoggingEmailTransport(ILogger<LoggingEmailTransport> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Password reset email dispatched to {Recipient} with subject {Subject}.",
            message.To,
            message.Subject);

        return Task.CompletedTask;
    }
}
