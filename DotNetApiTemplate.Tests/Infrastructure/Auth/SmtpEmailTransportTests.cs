using DotNetApiTemplate.Infrastructure.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.Auth;

public sealed class SmtpEmailTransportTests
{
    [Fact]
    public async Task SendAsync_WhenRequiredCredentialsAreMissing_ThrowsConfigurationError()
    {
        var transport = new SmtpEmailTransport(
            Options.Create(new MailSettings
            {
                SmtpServer = "smtps://smtp.office365.com:587",
                FromAddress = "noreply@example.com"
            }),
            NullLogger<SmtpEmailTransport>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SendAsync(
            new EmailMessage
            {
                To = "andy@example.com",
                Subject = "Reset password",
                Body = "Body"
            },
            CancellationToken.None));

        Assert.Equal("SmtpUserName is not configured.", exception.Message);
    }
}
