namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class EmailMessage
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? HtmlBody { get; init; }
}
