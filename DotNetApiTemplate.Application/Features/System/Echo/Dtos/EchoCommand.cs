using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetApiTemplate.Application.Features.System.Echo.Dtos;

public sealed class EchoCommand
{
    public string Message { get; init; } = string.Empty;
}
