using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests;
using DotNetApiTemplate.Api.Contracts.Responses;
using DotNetApiTemplate.Application.Features.System.Echo.Dtos;

namespace DotNetApiTemplate.Api.Mapping;

public sealed class SystemMappingProfile : Profile
{
    public SystemMappingProfile()
    {
        CreateMap<EchoRequest, EchoCommand>();
        CreateMap<EchoResult, EchoResponse>();
    }
}
