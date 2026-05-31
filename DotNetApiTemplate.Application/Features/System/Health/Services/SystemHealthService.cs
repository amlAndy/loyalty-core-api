using DotNetApiTemplate.Application.Features.System.Health.Dtos;
using DotNetApiTemplate.Application.Features.System.Health.Interfaces;

namespace DotNetApiTemplate.Application.Features.System.Health.Services;

public sealed class SystemHealthService : ISystemHealthService
{
    public SystemHealthDto GetHealth()
    {
        return new SystemHealthDto("Healthy");
    }
}
