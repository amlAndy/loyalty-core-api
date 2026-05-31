using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests.Auth;
using DotNetApiTemplate.Api.Contracts.Responses.Auth;
using DotNetApiTemplate.Application.Features.Auth.Dtos;

namespace DotNetApiTemplate.Api.Mapping;

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<LoginRequestDto, LoginCommand>();
        CreateMap<ForgotPasswordRequestDto, ForgotPasswordCommand>();
        CreateMap<ResetPasswordRequestDto, ResetPasswordCommand>();
        CreateMap<RefreshSessionRequestDto, RefreshSessionCommand>();
        CreateMap<SelectRoleRequestDto, SelectRoleCommand>();
        CreateMap<SsoLoginRequestDto, SsoLoginCommand>();

        CreateMap<AuthenticatedUserModel, AuthenticatedUserDto>();

        CreateMap<LoginResult, LoginResponseDto>()
            .ForMember(destination => destination.ExpiresAt, options => options.MapFrom(source => source.ExpiresAt.ToString("O")));

        CreateMap<AuthSessionResult, AuthSessionDto>()
            .ForMember(destination => destination.ExpiresAt, options => options.MapFrom(source => source.ExpiresAt.ToString("O")));

        CreateMap<SsoLoginResult, SsoLoginResultDto>()
            .ForMember(destination => destination.ExpiresAt, options => options.MapFrom(source => source.ExpiresAt.HasValue ? source.ExpiresAt.Value.ToString("O") : null));
    }
}
