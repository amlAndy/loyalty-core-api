using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Infrastructure.Auth;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.Auth;

public sealed class PasswordResetEmailSenderTests
{
    [Theory]
    [InlineData("Andy Morgan", null, "andy@example.com", "Andy")]
    [InlineData(null, "AndyUser Admin", "andy@example.com", "AndyUser")]
    [InlineData(null, null, "andy@example.com", "andy")]
    [InlineData(null, null, "", "Hello")]
    public async Task SendAsync_UsesExpectedGreeting(string? displayName, string? userName, string email, string expectedGreeting)
    {
        var transport = new CapturingEmailTransport();
        var sender = new PasswordResetEmailSender(
            Options.Create(new MailSettings
            {
                DevelopmentOverrideRecipient = "dev@example.com",
                PasswordResetSubject = "Reset password",
                PasswordResetBodyTemplate = "Greeting:{0};Link:{1}",
                FrontendBaseUrl = "https://frontend.example.com"
            }),
            new TestHostEnvironment { EnvironmentName = Environments.Development },
            transport,
            NullLogger<PasswordResetEmailSender>.Instance);

        await sender.SendAsync(
            new PasswordResetEmailModel
            {
                Email = email,
                DisplayName = displayName,
                UserName = userName,
                ResetLink = "https://frontend.example.com/auth/reset-password?token=abc"
            },
            CancellationToken.None);

        Assert.NotNull(transport.LastMessage);
        Assert.Equal(
            $"Greeting:{expectedGreeting};Link:https://frontend.example.com/auth/reset-password?token=abc",
            transport.LastMessage!.Body);
        Assert.NotNull(transport.LastMessage.HtmlBody);
        Assert.Contains($"Hello {expectedGreeting}", transport.LastMessage.HtmlBody);
        Assert.Contains("#4A2683", transport.LastMessage.HtmlBody);
    }

    private sealed class CapturingEmailTransport : IEmailTransport
    {
        public EmailMessage? LastMessage { get; private set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            LastMessage = message;
            return Task.CompletedTask;
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = nameof(DotNetApiTemplate.Tests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
