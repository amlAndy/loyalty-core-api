namespace DotNetApiTemplate.Api.Contracts.Responses.System;

public sealed record SystemInfoResponse(
    string ApplicationName,
    string EnvironmentName,
    string Version);
