namespace DotNetApiTemplate.Api.Contracts.Responses;

public sealed class EchoResponse
{
    public string OriginalMessage { get; init; } = string.Empty;
    public string EchoedMessage { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; }
}
