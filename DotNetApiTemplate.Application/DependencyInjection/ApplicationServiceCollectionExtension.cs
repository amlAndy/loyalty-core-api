using FluentValidation;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Audit.Services;
using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Services;
using Microsoft.Extensions.DependencyInjection;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Users.Services;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using DotNetApiTemplate.Application.Features.Auth.Services;
using DotNetApiTemplate.Application.Features.System.Echo.Interfaces;
using DotNetApiTemplate.Application.Features.System.Echo.Services;
using DotNetApiTemplate.Application.Features.System.Health.Interfaces;
using DotNetApiTemplate.Application.Features.System.Health.Services;
using DotNetApiTemplate.Application.Features.System.Info.Interfaces;
using DotNetApiTemplate.Application.Features.System.Info.Services;

namespace DotNetApiTemplate.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAdminRoleHierarchyPolicyService, AdminRoleHierarchyPolicyService>();
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddScoped<ISystemInfoService, SystemInfoService>();
        services.AddScoped<IEchoService, EchoService>();

        return services;
    }
}
