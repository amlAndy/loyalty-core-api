using System.Reflection;
using DotNetApiTemplate.Application.Features.System.Info.Dtos;
using DotNetApiTemplate.Application.Features.System.Info.Interfaces;
using Microsoft.Extensions.Hosting;

namespace DotNetApiTemplate.Application.Features.System.Info.Services;

public sealed class SystemInfoService : ISystemInfoService
{
    private readonly IHostEnvironment _hostEnvironment;

    public SystemInfoService(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public SystemInfoDto GetInfo()
    {
        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetName().Version?.ToString() ?? "Unknown";

        return new SystemInfoDto(
            ApplicationName: _hostEnvironment.ApplicationName,
            EnvironmentName: _hostEnvironment.EnvironmentName,
            Version: version);
    }
}
