using AutoMapper;
using DotNetApiTemplate.Api.Endpoints;
using DotNetApiTemplate.Application.DependencyInjection;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.DependencyInjection;

const string LocalAngularDevCorsPolicy = "LocalAngularDev";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalAngularDevCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:63065")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddAutoMapper(
    _ => { },
    typeof(Program).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IDevelopmentSsoTokenService, DevelopmentSsoTokenService>();
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentIdentitySeeder>();
    await seeder.SeedAsync();

    app.UseSwagger();
    app.UseSwaggerUI();
    // app.MapOpenApi();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(LocalAngularDevCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapDevelopmentSsoEndpoints();
    app.MapDevelopmentDataSeedEndpoints();
}

app.MapControllers();

app.Run();
