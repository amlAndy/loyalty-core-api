using DotNetApiTemplate.Api.Contracts.Requests.Development;
using DotNetApiTemplate.Api.Contracts.Responses.Development;
using DotNetApiTemplate.Application.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Endpoints;

public static class DevelopmentSsoEndpoints
{
    public static IEndpointRouteBuilder MapDevelopmentSsoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dev/sso")
            .WithTags("Development");

        group.MapPost("/token", async (
            [FromBody] DevelopmentSsoTokenRequestDto request,
            IDevelopmentSsoTokenService developmentSsoTokenService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["email"] = ["Email is required."]
                }));
            }

            var token = await developmentSsoTokenService.GenerateTokenAsync(request.Email, cancellationToken);

            if (token is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new DevelopmentSsoTokenResponseDto
            {
                Token = token
            });
        })
        .WithName("GenerateDevelopmentSsoToken")
        .WithSummary("Generates a development-only SSO token for a seeded user.")
        .WithDescription("Available only in Development. Returns a valid SSO token for a known seeded development user.")
        .Accepts<DevelopmentSsoTokenRequestDto>("application/json")
        .Produces<DevelopmentSsoTokenResponseDto>(StatusCodes.Status200OK)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
