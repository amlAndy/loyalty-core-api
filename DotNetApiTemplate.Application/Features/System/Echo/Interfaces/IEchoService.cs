using System;
using System.Collections.Generic;
using System.Text;
using DotNetApiTemplate.Application.Features.System.Echo.Dtos;

namespace DotNetApiTemplate.Application.Features.System.Echo.Interfaces;

public interface IEchoService
{
    Task<EchoResult> EchoAsync(
        EchoCommand command,
        CancellationToken cancellationToken = default);
}
