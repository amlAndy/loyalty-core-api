using DotNetApiTemplate.Application.Features.Admin.Audit;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Audit.Services;
using Xunit;

namespace DotNetApiTemplate.Application.Tests.Features.Admin.Audit.Services;

public sealed class AuditServiceTests
{
    [Fact]
    public async Task SearchAsync_defaults_page_values_and_date_range_when_missing()
    {
        var repository = new FakeAuditRepository();
        var service = new AuditService(repository);

        await service.SearchAsync(new AuditSearchCriteria());

        Assert.NotNull(repository.LastCriteria);
        Assert.Equal(1, repository.LastCriteria!.PageNumber);
        Assert.Equal(10, repository.LastCriteria.PageSize);
        Assert.False(string.IsNullOrWhiteSpace(repository.LastCriteria.DateFrom));
        Assert.False(string.IsNullOrWhiteSpace(repository.LastCriteria.DateTo));
    }

    [Fact]
    public async Task SearchAsync_trims_text_filters_and_preserves_valid_paging()
    {
        var repository = new FakeAuditRepository();
        var service = new AuditService(repository);

        await service.SearchAsync(new AuditSearchCriteria
        {
            SearchText = "  admin  ",
            Action = "  UserCreated ",
            EntityType = "  User ",
            ActingUser = "  Sydney Super User ",
            PageNumber = 2,
            PageSize = 25,
            DateFrom = "2026-04-20T00:00:00.0000000+00:00",
            DateTo = "2026-04-20T23:59:59.9990000+00:00"
        });

        Assert.NotNull(repository.LastCriteria);
        Assert.Equal("admin", repository.LastCriteria!.SearchText);
        Assert.Equal("UserCreated", repository.LastCriteria.Action);
        Assert.Equal("User", repository.LastCriteria.EntityType);
        Assert.Equal("Sydney Super User", repository.LastCriteria.ActingUser);
        Assert.Equal(2, repository.LastCriteria.PageNumber);
        Assert.Equal(25, repository.LastCriteria.PageSize);
    }

    [Fact]
    public async Task GetFilterOptionsAsync_returns_static_actions_and_entity_types_with_repository_users()
    {
        var repository = new FakeAuditRepository
        {
            ActingUsers = ["Ariana Admin", "Sydney Super User"]
        };
        var service = new AuditService(repository);

        var result = await service.GetFilterOptionsAsync();

        Assert.Equal(AuditActionTypes.Values.Length, result.Actions.Length);
        Assert.Contains(result.Actions, action => action.Value == AuditActionTypes.UserCreated && action.Label == "User Created");
        Assert.Equal(
            [AuditEntityTypes.User, AuditEntityTypes.Role, AuditEntityTypes.Review],
            result.EntityTypes.Select(entityType => entityType.Value).ToArray());
        Assert.Equal(
            ["Ariana Admin", "Sydney Super User"],
            result.ActingUsers.Select(user => user.Value).ToArray());
    }

    private sealed class FakeAuditRepository : IAuditRepository
    {
        public AuditSearchCriteria? LastCriteria { get; private set; }
        public string[] ActingUsers { get; init; } = [];

        public Task<AuditSearchResult> SearchAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(new AuditSearchResult
            {
                Items = [],
                Total = 0,
                PageNumber = criteria.PageNumber ?? 1,
                PageSize = criteria.PageSize ?? 10
            });
        }

        public Task<string[]> GetActingUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActingUsers);
        }
    }
}
