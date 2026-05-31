using System.Text.RegularExpressions;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;

namespace DotNetApiTemplate.Application.Features.Admin.Audit.Services;

public sealed class AuditService : IAuditService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;

    private readonly IAuditRepository _auditRepository;

    public AuditService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public Task<AuditSearchResult> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var normalizedCriteria = NormalizeCriteria(criteria);
        return _auditRepository.SearchAsync(normalizedCriteria, cancellationToken);
    }

    public async Task<AuditFilterOptionsModel> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        var actingUsers = await _auditRepository.GetActingUsersAsync(cancellationToken);

        return new AuditFilterOptionsModel
        {
            Actions = AuditActionTypes.Values
                .Select(action => new AuditFilterOptionModel
                {
                    Value = action,
                    Label = FormatAuditActionLabel(action)
                })
                .ToArray(),
            EntityTypes = AuditEntityTypes.Values
                .Select(entityType => new AuditFilterOptionModel
                {
                    Value = entityType,
                    Label = entityType
                })
                .ToArray(),
            ActingUsers = actingUsers
                .Select(user => new AuditFilterOptionModel
                {
                    Value = user,
                    Label = user
                })
                .ToArray()
        };
    }

    private static AuditSearchCriteria NormalizeCriteria(AuditSearchCriteria criteria)
    {
        var startOfDay = GetStartOfToday();
        var endOfDay = GetEndOfToday(startOfDay);

        return new AuditSearchCriteria
        {
            SearchText = criteria.SearchText?.Trim() ?? string.Empty,
            Action = criteria.Action?.Trim() ?? string.Empty,
            EntityType = criteria.EntityType?.Trim() ?? string.Empty,
            ActingUser = criteria.ActingUser?.Trim() ?? string.Empty,
            DateFrom = TryParseDate(criteria.DateFrom, out var dateFrom)
                ? dateFrom.ToString("O")
                : startOfDay.ToString("O"),
            DateTo = TryParseDate(criteria.DateTo, out var dateTo)
                ? dateTo.ToString("O")
                : endOfDay.ToString("O"),
            PageNumber = criteria.PageNumber.GetValueOrDefault() > 0
                ? criteria.PageNumber
                : DefaultPageNumber,
            PageSize = criteria.PageSize.GetValueOrDefault() > 0
                ? criteria.PageSize
                : DefaultPageSize
        };
    }

    private static string FormatAuditActionLabel(string action)
    {
        return Regex.Replace(action, "([a-z])([A-Z])", "$1 $2");
    }

    private static bool TryParseDate(string? value, out DateTimeOffset parsedDate)
    {
        return DateTimeOffset.TryParse(value, out parsedDate);
    }

    private static DateTimeOffset GetStartOfToday()
    {
        var now = DateTimeOffset.Now;
        return new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
    }

    private static DateTimeOffset GetEndOfToday(DateTimeOffset startOfDay)
    {
        return startOfDay.AddDays(1).AddMilliseconds(-1);
    }
}
