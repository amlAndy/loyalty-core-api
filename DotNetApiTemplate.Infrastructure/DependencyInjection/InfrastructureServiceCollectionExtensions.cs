using System.Text;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Infrastructure.Admin;
using DotNetApiTemplate.Infrastructure.Admin.Audit;
using DotNetApiTemplate.Infrastructure.Persistence;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Admin.Roles;
using DotNetApiTemplate.Infrastructure.Admin.Users;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DotNetApiTemplate.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LoyaltyCoreDb")
            ?? throw new InvalidOperationException("ConnectionStrings:LoyaltyCoreDb is not configured.");
        var jwtIssuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var jwtAudience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddHttpContextAccessor();
        services.Configure<MailSettings>(configuration.GetSection("MailSettings"));

        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AdminAuthorizationPolicies.AdminAccess,
                policy => policy.RequireRole(ApplicationRoleNames.AdminRoles));
        });

        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAdminMutationScope, AdminMutationScope>();
        services.AddScoped<IAdminRoleRepository, AdminRoleRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<LoveBrackleyService>();
        services.AddScoped<IMemberLoyaltyService>(provider => provider.GetRequiredService<LoveBrackleyService>());
        services.AddScoped<ILoveBrackleyLookupService>(provider => provider.GetRequiredService<LoveBrackleyService>());
        services.AddScoped<IParticipatingBusinessService>(provider => provider.GetRequiredService<LoveBrackleyService>());
        services.AddScoped<IBusinessLoyaltyService>(provider => provider.GetRequiredService<LoveBrackleyService>());
        services.AddScoped<IAdminLoveBrackleyService>(provider => provider.GetRequiredService<LoveBrackleyService>());
        services.AddSingleton<IPermissionCatalogProvider, StaticPermissionCatalogProvider>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IEmailTransport, SmtpEmailTransport>();
        services.AddScoped<IPasswordResetLinkBuilder, PasswordResetLinkBuilder>();
        services.AddScoped<IPasswordResetEmailSender, PasswordResetEmailSender>();
        services.AddScoped<IUserAuthRepository, IdentityUserAuthRepository>();
        services.AddScoped<ISsoTokenProcessor, SsoTokenProcessor>();
        services.AddScoped<DevelopmentIdentitySeeder>();

        return services;
    }
}
