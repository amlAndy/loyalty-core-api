namespace DotNetApiTemplate.Infrastructure.Auth;

public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
