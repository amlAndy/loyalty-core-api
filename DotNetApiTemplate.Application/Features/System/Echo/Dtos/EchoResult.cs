using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetApiTemplate.Application.Features.System.Echo.Dtos;

public sealed class EchoResult
{
    public string OriginalMessage { get; init; } = string.Empty;

    public string EchoedMessage { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; }
}
