namespace DotNetApiTemplate.Api.Contracts.Requests;

public sealed class EchoRequest
{
    public string Message { get; init; } = string.Empty;
}
