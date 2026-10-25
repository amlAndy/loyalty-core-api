using System.Text.Json;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using Xunit;

namespace DotNetApiTemplate.Tests.Api.Contracts;

public sealed class BusinessSettingsRequestContractTests
{
    [Fact]
    public void BusinessOwnerSettingsRequest_ExtraFeaturedField_IsIgnored()
    {
        const string json = """
            {
              "name": "Green Pantry",
              "category": "groceries",
              "description": "Updated",
              "locationName": "Market",
              "addressLine1": "1 Market Place",
              "addressLine2": null,
              "town": "brackley",
              "postcode": "NN13 7AA",
              "contactEmail": "hello@example.com",
              "supportEmail": null,
              "primaryContactName": "Owner",
              "tradingHoursSummary": "Mon-Fri 09:00-17:00",
              "featured": true
            }
            """;

        var request = JsonSerializer.Deserialize<UpdateBusinessSettingsRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Equal("Green Pantry", request!.Name);
        Assert.Equal("groceries", request.Category);
    }

    [Fact]
    public void AdminBusinessSettingsRequest_IncludesFeaturedField()
    {
        const string json = """
            {
              "name": "Green Pantry",
              "category": "groceries",
              "description": "Updated",
              "locationName": "Market",
              "addressLine1": "1 Market Place",
              "addressLine2": null,
              "town": "brackley",
              "postcode": "NN13 7AA",
              "contactEmail": "hello@example.com",
              "supportEmail": null,
              "primaryContactName": "Owner",
              "tradingHoursSummary": "Mon-Fri 09:00-17:00",
              "featured": true
            }
            """;

        var request = JsonSerializer.Deserialize<AdminUpdateBusinessSettingsRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.True(request!.Featured);
    }
}
