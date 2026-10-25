using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.LoveBrackley;

public sealed class LoveBrackleyServiceBusinessSettingsLookupTests
{
    [Fact]
    public async Task UpdateSettingsAsync_AcceptsCategorySlugAndTownDisplayName_AndKeepsLegacyAndLookupConsistent()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(dbContext, ids.ActiveTownId, ids.ActiveCategoryId, category: "Deli", town: "Old Town Name");
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);
        var request = CreateUpdateRequest(category: "groceries", town: "Brackley");

        var result = await service.UpdateSettingsAsync(businessId, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ids.ActiveCategoryId, result!.BusinessCategoryId);
        Assert.Equal(ids.ActiveTownId, result.TownId);
        Assert.Equal("Groceries", result.Category);
        Assert.Equal("Groceries", result.CategoryName);
        Assert.Equal("groceries", result.CategorySlug);
        Assert.Equal("Brackley", result.Town);
        Assert.Equal("Brackley", result.TownName);
        Assert.Equal("brackley", result.TownSlug);

        var persisted = await dbContext.BusinessProfiles.SingleAsync(candidate => candidate.Id == businessId);
        Assert.Equal(ids.ActiveCategoryId, persisted.BusinessCategoryId);
        Assert.Equal(ids.ActiveTownId, persisted.TownId);
        Assert.Equal("Groceries", persisted.Category);
        Assert.Equal("Brackley", persisted.Town);
    }

    [Fact]
    public async Task UpdateBusinessAsync_AcceptsCategoryNameAndTownSlug_AndKeepsLegacyLookupAndFeaturedConsistent()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(dbContext, ids.ActiveTownId, ids.ActiveCategoryId);
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);
        var request = CreateAdminUpdateRequest(category: "Groceries", town: "brackley", featured: false);

        var result = await service.UpdateBusinessAsync(businessId, request, "admin-actor", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ids.ActiveCategoryId, result!.BusinessCategoryId);
        Assert.Equal(ids.ActiveTownId, result.TownId);
        Assert.Equal("Groceries", result.Category);
        Assert.Equal("Brackley", result.Town);
        Assert.False(result.Featured);

        var persisted = await dbContext.BusinessProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == businessId);
        Assert.False(persisted.Featured);
    }

    [Fact]
    public async Task UpdateSettingsAsync_PreservesExistingFeaturedFalse_ForBusinessSettingsSave()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(dbContext, ids.ActiveTownId, ids.ActiveCategoryId);
        await dbContext.SaveChangesAsync();

        var seeded = await dbContext.BusinessProfiles.SingleAsync(candidate => candidate.Id == businessId);
        seeded.Featured = false;
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var result = await service.UpdateSettingsAsync(
            businessId,
            CreateUpdateRequest(category: "groceries", town: "brackley", description: "Owner edited description"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Featured);

        var persisted = await dbContext.BusinessProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == businessId);
        Assert.False(persisted.Featured);
        Assert.Equal("Owner edited description", persisted.Description);
    }

    [Fact]
    public async Task UpdateSettingsAsync_PreservesExistingFeaturedTrue_ForBusinessSettingsSave()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(dbContext, ids.ActiveTownId, ids.ActiveCategoryId);
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var result = await service.UpdateSettingsAsync(
            businessId,
            CreateUpdateRequest(category: "groceries", town: "brackley", description: "Owner edited description"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.Featured);

        var persisted = await dbContext.BusinessProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == businessId);
        Assert.True(persisted.Featured);
        Assert.Equal("Owner edited description", persisted.Description);
    }

    [Fact]
    public async Task UpdateSettingsAsync_InvalidCategory_ThrowsValidationAndDoesNotPartiallySave()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(dbContext, ids.ActiveTownId, ids.ActiveCategoryId);
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);
        var original = await dbContext.BusinessProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == businessId);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSettingsAsync(businessId, CreateUpdateRequest(category: "no-such-category", town: "Brackley"), CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "category" && error.ErrorMessage == "A valid business category is required.");

        var persisted = await dbContext.BusinessProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == businessId);
        Assert.Equal(original.Category, persisted.Category);
        Assert.Equal(original.Town, persisted.Town);
        Assert.Equal(original.Description, persisted.Description);
        Assert.Equal(original.BusinessCategoryId, persisted.BusinessCategoryId);
        Assert.Equal(original.TownId, persisted.TownId);
    }

    [Fact]
    public async Task UpdateSettingsAsync_AmbiguousCategoryName_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(dbContext, ids.ActiveTownId, ids.ActiveCategoryId);
        dbContext.BusinessCategories.Add(new BusinessCategoryEntity
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000098"),
            Name = "Groceries",
            Slug = "groceries-duplicate",
            Description = "Duplicate for ambiguity test",
            IsActive = true,
            DisplayOrder = 99,
            CreatedUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSettingsAsync(businessId, CreateUpdateRequest(category: "Groceries", town: "Brackley"), CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "category" && error.ErrorMessage.Contains("ambiguous", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateSettingsAsync_AmbiguousTownDisplayName_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(dbContext, ids.ActiveTownId, ids.ActiveCategoryId);
        dbContext.Towns.Add(new TownEntity
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000099"),
            Name = "Brackley North",
            Slug = "brackley-north",
            DisplayName = "Brackley",
            PrimaryDomain = null,
            IsActive = true,
            DisplayOrder = 99,
            CreatedUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSettingsAsync(businessId, CreateUpdateRequest(category: "Groceries", town: "Brackley"), CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "town" && error.ErrorMessage.Contains("ambiguous", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenCurrentLookupsAreInactive_AllowsUnrelatedEditWithoutReassignment()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(
            dbContext,
            ids.InactiveTownId,
            ids.InactiveCategoryId,
            category: "Legacy Deli",
            town: "Old Brackley",
            description: "Before");
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);
        var request = CreateUpdateRequest(category: "Legacy Deli", town: "Old Brackley", description: "After");

        var result = await service.UpdateSettingsAsync(businessId, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ids.InactiveCategoryId, result!.BusinessCategoryId);
        Assert.Equal(ids.InactiveTownId, result.TownId);
        Assert.Equal("Legacy Deli", result.Category);
        Assert.Equal("Old Brackley", result.Town);
        Assert.Equal("After", result.Description);

        var persisted = await dbContext.BusinessProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == businessId);
        Assert.Equal(ids.InactiveCategoryId, persisted.BusinessCategoryId);
        Assert.Equal(ids.InactiveTownId, persisted.TownId);
        Assert.Equal("Legacy Deli", persisted.Category);
        Assert.Equal("Old Brackley", persisted.Town);
        Assert.Equal("After", persisted.Description);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ExistingDeliGroceriesMismatch_RequiresExplicitLookupReconciliation()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        var businessId = SeedBusiness(dbContext, ids.ActiveTownId, ids.ActiveCategoryId, category: "Deli", town: "Brackley", description: "Before");
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);
        var request = CreateUpdateRequest(category: "Deli", town: "Brackley", description: "After");

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSettingsAsync(businessId, request, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "category" && error.ErrorMessage == "A valid business category is required.");

        var persisted = await dbContext.BusinessProfiles.AsNoTracking().SingleAsync(candidate => candidate.Id == businessId);
        Assert.Equal("Deli", persisted.Category);
        Assert.Equal("Before", persisted.Description);
        Assert.Equal(ids.ActiveCategoryId, persisted.BusinessCategoryId);
    }

    [Fact]
    public async Task CreateBusinessAsync_AcceptsActiveLookupSelectors_AndStoresCanonicalStrings()
    {
        await using var dbContext = CreateDbContext();
        var ids = SeedLookups(dbContext);
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var result = await service.CreateBusinessAsync(
            new CreateAdminBusinessRequest(
                OwnerUserId: "owner-user-2",
                Name: "Fresh Market",
                BrandReference: "LB-BIZ-NEW",
                Category: "groceries",
                Description: "Desc",
                LocationName: "Location",
                AddressLine1: "1 Test Street",
                AddressLine2: null,
                Town: "Brackley",
                Postcode: "NN13 7AA",
                ContactEmail: "fresh@example.com",
                SupportEmail: null,
                PrimaryContactName: "Owner",
                TradingHoursSummary: "Mon-Fri 09:00-17:00",
                Featured: false),
            actorUserId: "admin-actor",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ids.ActiveCategoryId, result!.BusinessCategoryId);
        Assert.Equal(ids.ActiveTownId, result.TownId);
        Assert.Equal("Groceries", result.Category);
        Assert.Equal("Brackley", result.TownName);
    }

    [Fact]
    public async Task CreateBusinessAsync_InactiveLookupSelector_IsRejected()
    {
        await using var dbContext = CreateDbContext();
        SeedLookups(dbContext);
        await dbContext.SaveChangesAsync();

        var service = new LoveBrackleyService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateBusinessAsync(
                new CreateAdminBusinessRequest(
                    OwnerUserId: "owner-user-3",
                    Name: "Legacy Market",
                    BrandReference: "LB-BIZ-LEG",
                    Category: "Legacy Deli",
                    Description: "Desc",
                    LocationName: "Location",
                    AddressLine1: "1 Test Street",
                    AddressLine2: null,
                    Town: "Old Brackley",
                    Postcode: "NN13 7AA",
                    ContactEmail: "legacy@example.com",
                    SupportEmail: null,
                    PrimaryContactName: "Owner",
                    TradingHoursSummary: "Mon-Fri 09:00-17:00",
                    Featured: false),
                actorUserId: "admin-actor",
                cancellationToken: CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName is "category" or "town");
    }

    private static UpdateBusinessSettingsRequest CreateUpdateRequest(string category, string town, string description = "Updated description")
    {
        return new UpdateBusinessSettingsRequest(
            Name: "Green Pantry",
            Category: category,
            Description: description,
            LocationName: "High Street",
            AddressLine1: "1 High Street",
            AddressLine2: null,
            Town: town,
            Postcode: "NN13 7AA",
            ContactEmail: "contact@example.com",
            SupportEmail: null,
            PrimaryContactName: "Jamie Owner",
            TradingHoursSummary: "Mon-Fri 09:00-17:00");
    }

    private static AdminUpdateBusinessSettingsRequest CreateAdminUpdateRequest(string category, string town, bool featured, string description = "Updated description")
    {
        return new AdminUpdateBusinessSettingsRequest(
            Name: "Green Pantry",
            Category: category,
            Description: description,
            LocationName: "High Street",
            AddressLine1: "1 High Street",
            AddressLine2: null,
            Town: town,
            Postcode: "NN13 7AA",
            ContactEmail: "contact@example.com",
            SupportEmail: null,
            PrimaryContactName: "Jamie Owner",
            TradingHoursSummary: "Mon-Fri 09:00-17:00",
            Featured: featured);
    }

    private static Guid SeedBusiness(ApplicationDbContext dbContext, Guid townId, Guid categoryId, string category = "Groceries", string town = "Brackley", string description = "Original description")
    {
        var businessId = Guid.NewGuid();
        dbContext.BusinessProfiles.Add(new BusinessProfileEntity
        {
            Id = businessId,
            OwnerUserId = "owner-user-1",
            Name = "Green Pantry",
            BrandReference = $"LB-BIZ-{Guid.NewGuid():N}"[..20],
            Category = category,
            TownId = townId,
            BusinessCategoryId = categoryId,
            Description = description,
            LocationName = "Market Place",
            AddressLine1 = "15 Market Place",
            AddressLine2 = null,
            Town = town,
            Postcode = "NN13 7AJ",
            ContactEmail = "owner@example.com",
            SupportEmail = null,
            PrimaryContactName = "Jamie Owner",
            TradingHoursSummary = "Mon-Sat 08:00-18:00",
            ParticipationStatus = "Active",
            Featured = true,
            CreatedUtc = DateTimeOffset.UtcNow.AddDays(-10),
            UpdatedUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });

        return businessId;
    }

    private static (Guid ActiveTownId, Guid InactiveTownId, Guid ActiveCategoryId, Guid InactiveCategoryId) SeedLookups(ApplicationDbContext dbContext)
    {
        var activeTownId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var inactiveTownId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var activeCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000007");
        var inactiveCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000099");

        dbContext.Towns.AddRange(
            new TownEntity
            {
                Id = activeTownId,
                Name = "Brackley",
                Slug = "brackley",
                DisplayName = "Brackley",
                PrimaryDomain = null,
                IsActive = true,
                DisplayOrder = 1,
                CreatedUtc = DateTimeOffset.UtcNow.AddYears(-1)
            },
            new TownEntity
            {
                Id = inactiveTownId,
                Name = "Old Brackley",
                Slug = "old-brackley",
                DisplayName = "Old Brackley",
                PrimaryDomain = null,
                IsActive = false,
                DisplayOrder = 2,
                CreatedUtc = DateTimeOffset.UtcNow.AddYears(-1)
            });

        dbContext.BusinessCategories.AddRange(
            new BusinessCategoryEntity
            {
                Id = activeCategoryId,
                Name = "Groceries",
                Slug = "groceries",
                Description = "Groceries",
                IsActive = true,
                DisplayOrder = 1,
                CreatedUtc = DateTimeOffset.UtcNow.AddYears(-1)
            },
            new BusinessCategoryEntity
            {
                Id = inactiveCategoryId,
                Name = "Legacy Deli",
                Slug = "legacy-deli",
                Description = "Legacy",
                IsActive = false,
                DisplayOrder = 2,
                CreatedUtc = DateTimeOffset.UtcNow.AddYears(-1)
            });

        return (activeTownId, inactiveTownId, activeCategoryId, inactiveCategoryId);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"love-brackley-business-settings-lookups-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
