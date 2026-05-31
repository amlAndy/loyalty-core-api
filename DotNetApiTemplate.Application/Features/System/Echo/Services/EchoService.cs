using DotNetApiTemplate.Application.Features.System.Echo.Dtos;
using DotNetApiTemplate.Application.Features.System.Echo.Interfaces;

namespace DotNetApiTemplate.Application.Features.System.Echo.Services;

public sealed class EchoService : IEchoService
{
    public Task<EchoResult> EchoAsync(
        EchoCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new EchoResult
        {
            OriginalMessage = command.Message,
            EchoedMessage = command.Message,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        return Task.FromResult(result);
    }
}
