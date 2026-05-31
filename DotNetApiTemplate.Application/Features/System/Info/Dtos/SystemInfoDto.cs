namespace DotNetApiTemplate.Application.Features.System.Info.Dtos;

public sealed record SystemInfoDto(
    string ApplicationName,
    string EnvironmentName,
    string Version);
