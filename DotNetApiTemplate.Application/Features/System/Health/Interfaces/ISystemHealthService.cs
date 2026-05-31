using DotNetApiTemplate.Application.Features.System.Health.Dtos;

namespace DotNetApiTemplate.Application.Features.System.Health.Interfaces;

public interface ISystemHealthService
{
    SystemHealthDto GetHealth();
}
