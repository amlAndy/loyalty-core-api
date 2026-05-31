using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests;
using DotNetApiTemplate.Api.Contracts.Responses;
using DotNetApiTemplate.Api.Contracts.Responses.System;
using DotNetApiTemplate.Application.Features.System.Echo.Dtos;
using DotNetApiTemplate.Application.Features.System.Echo.Interfaces;
using DotNetApiTemplate.Application.Features.System.Health.Interfaces;
using DotNetApiTemplate.Application.Features.System.Info.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SystemController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly ISystemHealthService _systemHealthService;
    private readonly ISystemInfoService _systemInfoService;
    private readonly IEchoService _echoService;

    public SystemController(
        IMapper mapper,
        ISystemHealthService systemHealthService,
        ISystemInfoService systemInfoService,
        IEchoService echoService)
    {
        _mapper = mapper;
        _systemHealthService = systemHealthService;
        _systemInfoService = systemInfoService;
        _echoService = echoService;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemHealthResponse> GetHealth()
    {
        var health = _systemHealthService.GetHealth();

        return Ok(new SystemHealthResponse(
            Status: health.Status));
    }

    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> GetInfo()
    {
        var info = _systemInfoService.GetInfo();

        return Ok(new SystemInfoResponse(
            ApplicationName: info.ApplicationName,
            EnvironmentName: info.EnvironmentName,
            Version: info.Version));
    }

    [HttpPost("echo")]
    [ProducesResponseType(typeof(EchoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EchoResponse>> Echo(
    [FromBody] EchoRequest request,
    CancellationToken cancellationToken)
    {
        var command = _mapper.Map<EchoCommand>(request);

        var result = await _echoService.EchoAsync(command, cancellationToken);

        var response = _mapper.Map<EchoResponse>(result);

        return Ok(response);
    }
}
