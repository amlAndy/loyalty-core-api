using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

internal static class LoveBrackleyControllerHelpers
{
    public static string? GetCurrentUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("nameid")
            ?? user.FindFirstValue("sub");
    }

    public static string? GetActiveRoleId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue("active_role_id");
    }

    public static string? GetBearerToken(this HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..].Trim()
            : null;
    }

    public static ActionResult CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "request" : error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return new BadRequestObjectResult(new ValidationProblemDetails(errors));
    }
}
