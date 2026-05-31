using System.Text.Json;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Admin.Audit;

public sealed class AuditRepository : IAuditRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AuditRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditSearchResult> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var auditQuery = _dbContext.AuditEntries
            .AsNoTracking()
            .Include(entry => entry.Details)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var normalizedSearchText = criteria.SearchText.Trim().ToUpper();
            auditQuery = auditQuery.Where(entry =>
                entry.ActingUser.ToUpper().Contains(normalizedSearchText) ||
                entry.ActionLabel.ToUpper().Contains(normalizedSearchText) ||
                entry.EntityType.ToUpper().Contains(normalizedSearchText) ||
                entry.TargetName.ToUpper().Contains(normalizedSearchText) ||
                entry.Summary.ToUpper().Contains(normalizedSearchText) ||
                entry.Details.Any(detail =>
                    detail.Label.ToUpper().Contains(normalizedSearchText) ||
                    (detail.BeforeValueJson ?? string.Empty).ToUpper().Contains(normalizedSearchText) ||
                    (detail.AfterValueJson ?? string.Empty).ToUpper().Contains(normalizedSearchText)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action))
        {
            var action = criteria.Action.Trim();
            auditQuery = auditQuery.Where(entry => entry.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            var entityType = criteria.EntityType.Trim();
            auditQuery = auditQuery.Where(entry => entry.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ActingUser))
        {
            var actingUser = criteria.ActingUser.Trim();
            auditQuery = auditQuery.Where(entry => entry.ActingUser == actingUser);
        }

        if (DateTimeOffset.TryParse(criteria.DateFrom, out var dateFrom))
        {
            auditQuery = auditQuery.Where(entry => entry.OccurredAtUtc >= dateFrom);
        }

        if (DateTimeOffset.TryParse(criteria.DateTo, out var dateTo))
        {
            auditQuery = auditQuery.Where(entry => entry.OccurredAtUtc <= dateTo);
        }

        var pageNumber = criteria.PageNumber ?? 1;
        var pageSize = criteria.PageSize ?? 10;
        var total = await auditQuery.CountAsync(cancellationToken);

        if (total == 0)
        {
            return new AuditSearchResult
            {
                Items = [],
                Total = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        var items = await auditQuery
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new AuditEntryModel
            {
                Id = entry.Id.ToString(),
                OccurredAt = entry.OccurredAtUtc.ToString("O"),
                ActingUser = entry.ActingUser,
                Action = entry.Action,
                ActionLabel = entry.ActionLabel,
                EntityType = entry.EntityType,
                TargetId = entry.TargetId,
                TargetName = entry.TargetName,
                Target = entry.Target,
                Summary = entry.Summary,
                Details = entry.Details
                    .OrderBy(detail => detail.Sequence)
                    .Select(detail => new AuditChangeDetailModel
                    {
                        Field = detail.Field,
                        Label = detail.Label,
                        BeforeValue = ParseStoredValue(detail.BeforeValueJson),
                        AfterValue = ParseStoredValue(detail.AfterValueJson)
                    })
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        return new AuditSearchResult
        {
            Items = items,
            Total = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<string[]> GetActingUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditEntries
            .AsNoTracking()
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ActingUser))
            .Select(entry => entry.ActingUser)
            .Distinct()
            .OrderBy(user => user)
            .ToArrayAsync(cancellationToken);
    }

    private static object? ParseStoredValue(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return root.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => root.GetString(),
            JsonValueKind.Array => root.EnumerateArray().Select(item => item.ToString()).ToArray(),
            _ => root.ToString()
        };
    }
}
