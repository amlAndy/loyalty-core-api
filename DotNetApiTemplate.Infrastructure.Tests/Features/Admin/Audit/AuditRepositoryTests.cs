using System.Text.Json;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Infrastructure.Admin.Audit;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotNetApiTemplate.Infrastructure.Tests.Features.Admin.Audit;

public sealed class AuditRepositoryTests
{
    [Fact]
    public async Task SearchAsync_filters_pages_and_returns_total_before_paging()
    {
        await using var dbContext = CreateDbContext();
        SeedAuditEntries(dbContext);
        var repository = new AuditRepository(dbContext);

        var result = await repository.SearchAsync(new AuditSearchCriteria
        {
            SearchText = "admin",
            EntityType = "User",
            DateFrom = "2026-04-20T00:00:00.0000000+00:00",
            DateTo = "2026-04-30T23:59:59.9990000+00:00",
            PageNumber = 2,
            PageSize = 1
        });

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal("11111111-1111-1111-1111-111111111111", result.Items[0].Id);
        Assert.Equal("Sydney Super User", result.Items[0].ActingUser);
    }

    [Fact]
    public async Task SearchAsync_returns_detail_values_as_strings_and_arrays()
    {
        await using var dbContext = CreateDbContext();
        SeedAuditEntries(dbContext);
        var repository = new AuditRepository(dbContext);

        var result = await repository.SearchAsync(new AuditSearchCriteria
        {
            Action = "UserCreated",
            DateFrom = "2026-04-20T00:00:00.0000000+00:00",
            DateTo = "2026-04-30T23:59:59.9990000+00:00",
            PageNumber = 1,
            PageSize = 10
        });

        var entry = Assert.Single(result.Items);
        Assert.Equal("Ariana Admin", entry.TargetName);
        Assert.Equal(2, entry.Details.Length);
        Assert.Equal("Finance child role", Assert.IsType<string>(entry.Details[0].AfterValue));
        Assert.Equal(["Administrator", "Member"], Assert.IsType<string[]>(entry.Details[1].AfterValue));
    }

    [Fact]
    public async Task GetActingUsersAsync_returns_distinct_sorted_users()
    {
        await using var dbContext = CreateDbContext();
        SeedAuditEntries(dbContext);
        var repository = new AuditRepository(dbContext);

        var result = await repository.GetActingUsersAsync();

        Assert.Equal(["Ariana Admin", "Sydney Super User"], result);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SeedAuditEntries(ApplicationDbContext dbContext)
    {
        dbContext.AuditEntries.AddRange(
        [
            new AuditEntryEntity
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                OccurredAtUtc = DateTimeOffset.Parse("2026-04-22T10:15:00Z"),
                ActingUser = "Sydney Super User",
                Action = "UserCreated",
                ActionLabel = "User Created",
                EntityType = "User",
                TargetId = "user-001",
                TargetName = "Ariana Admin",
                Target = "Ariana Admin",
                Summary = "Created an administrator account with Finance child role.",
                Details =
                [
                    new AuditChangeDetailEntity
                    {
                        Id = Guid.Parse("21111111-1111-1111-1111-111111111111"),
                        Sequence = 0,
                        Field = "financeRole",
                        Label = "Finance role",
                        BeforeValueJson = null,
                        AfterValueJson = JsonSerializer.Serialize("Finance child role")
                    },
                    new AuditChangeDetailEntity
                    {
                        Id = Guid.Parse("31111111-1111-1111-1111-111111111111"),
                        Sequence = 1,
                        Field = "roles",
                        Label = "Roles",
                        BeforeValueJson = JsonSerializer.Serialize(Array.Empty<string>()),
                        AfterValueJson = JsonSerializer.Serialize(new[] { "Administrator", "Member" })
                    }
                ]
            },
            new AuditEntryEntity
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                OccurredAtUtc = DateTimeOffset.Parse("2026-04-24T14:45:00Z"),
                ActingUser = "Sydney Super User",
                Action = "RoleUpdated",
                ActionLabel = "Role Updated",
                EntityType = "Role",
                TargetId = "role-administrator",
                TargetName = "Administrator",
                Target = "Administrator",
                Summary = "Updated Administrator example permissions under the protected Super User root.",
                Details = []
            },
            new AuditEntryEntity
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                OccurredAtUtc = DateTimeOffset.Parse("2026-04-26T08:30:00Z"),
                ActingUser = "Ariana Admin",
                Action = "UserStatusChanged",
                ActionLabel = "User Status Changed",
                EntityType = "User",
                TargetId = "user-002",
                TargetName = "Sam Supplier",
                Target = "Sam Supplier",
                Summary = "Returned supplier account to active status for testing.",
                Details = []
            }
        ]);

        dbContext.SaveChanges();
    }
}
