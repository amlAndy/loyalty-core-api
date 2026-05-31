using System.Text.Json;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DotNetApiTemplate.Infrastructure.Admin.Audit;

public sealed class AuditWriter : IAuditWriter
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditWriter(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task WriteAsync(AuditWriteEntryModel entry, CancellationToken cancellationToken = default)
    {
        var actorId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorName = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

        var auditEntry = new AuditEntryEntity
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ActingUserId = string.IsNullOrWhiteSpace(actorId) ? null : actorId,
            ActingUser = string.IsNullOrWhiteSpace(actorName) ? "System" : actorName,
            Action = entry.Action,
            ActionLabel = entry.ActionLabel,
            EntityType = entry.EntityType,
            TargetId = entry.TargetId,
            TargetName = entry.TargetName,
            Target = entry.Target,
            Summary = entry.Summary,
            Details = entry.Details
                .Select((detail, index) => new AuditChangeDetailEntity
                {
                    Id = Guid.NewGuid(),
                    Sequence = index,
                    Field = detail.Field,
                    Label = detail.Label,
                    BeforeValueJson = SerializeValue(detail.BeforeValue),
                    AfterValueJson = SerializeValue(detail.AfterValue)
                })
                .ToArray()
        };

        _dbContext.AuditEntries.Add(auditEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? SerializeValue(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value);
    }
}
