using DotNetApiTemplate.Application.Features.System.Info.Dtos;

namespace DotNetApiTemplate.Application.Features.System.Info.Interfaces;

public interface ISystemInfoService
{
    SystemInfoDto GetInfo();
}
