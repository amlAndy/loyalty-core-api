using System.Text.Json;
using DotNetApiTemplate.Application.Features.Admin.Audit;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using DotNetApiTemplate.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace DotNetApiTemplate.Tests.Infrastructure.LoveBrackley;

public sealed class LoveBrackleyServiceReviewWorkflowTests
{
    [Theory]
    [InlineData("Open", true)]
    [InlineData("InReview", true)]
    [InlineData("Escalated", true)]
    [InlineData("Resolved", false)]
    public async Task ResolveReviewAsync_enforces_transition_matrix_and_audits_successes(string initialStatus, bool allowed)
    {
        await using var dbContext = CreateDbContext();
        var reviewId = await SeedReviewAsync(dbContext, initialStatus, notes: "Original notes", resolvedUtc: initialStatus == "Resolved" ? DateTimeOffset.UtcNow.AddHours(-1) : null);
        var service = new LoveBrackleyService(dbContext);

        if (allowed)
        {
            var result = await service.ResolveReviewAsync(reviewId, new ResolveReviewCaseRequest("Resolved notes"), "actor-1", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Resolved", result!.Status);
            Assert.Equal("Resolved notes", result.ResolutionNotes);
            Assert.NotNull(result.ResolvedUtc);

            var stored = await dbContext.AdminReviewCases.SingleAsync(candidate => candidate.Id == reviewId);
            Assert.Equal("Resolved", stored.Status);
            Assert.Equal("Resolved notes", stored.ResolutionNotes);
            Assert.NotNull(stored.ResolvedUtc);

            var audit = await dbContext.AdminAuditLogs.SingleAsync();
            Assert.Equal("actor-1", audit.ActorUserId);
            Assert.Equal(AuditEntityTypes.Review, audit.EntityType);
            Assert.Equal(reviewId.ToString(), audit.EntityId);
            Assert.Equal(AuditActionTypes.ReviewResolved, audit.Action);
            Assert.Equal("Resolved notes", audit.Reason);
            Assert.Contains("Resolved review case", audit.Summary);
            AssertReviewAuditState(audit.BeforeJson, "Open" == initialStatus ? "Open" : initialStatus, "Original notes", null);
            AssertReviewAuditState(audit.AfterJson, "Resolved", "Resolved notes", true);
        }
        else
        {
            await Assert.ThrowsAsync<ValidationException>(() => service.ResolveReviewAsync(reviewId, new ResolveReviewCaseRequest("Resolved notes"), "actor-1", CancellationToken.None));

            var stored = await dbContext.AdminReviewCases.SingleAsync(candidate => candidate.Id == reviewId);
            Assert.Equal("Resolved", stored.Status);
            Assert.Equal("Original notes", stored.ResolutionNotes);
            Assert.NotNull(stored.ResolvedUtc);
            Assert.Empty(dbContext.AdminAuditLogs);
        }
    }

    [Theory]
    [InlineData("Open", true)]
    [InlineData("InReview", true)]
    [InlineData("Escalated", false)]
    [InlineData("Resolved", false)]
    public async Task EscalateReviewAsync_enforces_transition_matrix_and_audits_successes(string initialStatus, bool allowed)
    {
        await using var dbContext = CreateDbContext();
        var reviewId = await SeedReviewAsync(dbContext, initialStatus, notes: "Original notes", resolvedUtc: initialStatus == "Resolved" ? DateTimeOffset.UtcNow.AddHours(-1) : null);
        var service = new LoveBrackleyService(dbContext);

        if (allowed)
        {
            var result = await service.EscalateReviewAsync(reviewId, new EscalateReviewCaseRequest("Escalation reason"), "actor-2", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Escalated", result!.Status);
            Assert.Equal("Escalation reason", result.ResolutionNotes);
            Assert.Null(result.ResolvedUtc);

            var stored = await dbContext.AdminReviewCases.SingleAsync(candidate => candidate.Id == reviewId);
            Assert.Equal("Escalated", stored.Status);
            Assert.Equal("Escalation reason", stored.ResolutionNotes);
            Assert.Null(stored.ResolvedUtc);

            var audit = await dbContext.AdminAuditLogs.SingleAsync();
            Assert.Equal("actor-2", audit.ActorUserId);
            Assert.Equal(AuditEntityTypes.Review, audit.EntityType);
            Assert.Equal(reviewId.ToString(), audit.EntityId);
            Assert.Equal(AuditActionTypes.ReviewEscalated, audit.Action);
            Assert.Equal("Escalation reason", audit.Reason);
            Assert.Contains("Escalated review case", audit.Summary);
            AssertReviewAuditState(audit.BeforeJson, initialStatus, "Original notes", initialStatus == "Resolved");
            AssertReviewAuditState(audit.AfterJson, "Escalated", "Escalation reason", false);
        }
        else
        {
            await Assert.ThrowsAsync<ValidationException>(() => service.EscalateReviewAsync(reviewId, new EscalateReviewCaseRequest("Escalation reason"), "actor-2", CancellationToken.None));

            var stored = await dbContext.AdminReviewCases.SingleAsync(candidate => candidate.Id == reviewId);
            Assert.Equal(initialStatus, stored.Status);
            Assert.Equal("Original notes", stored.ResolutionNotes);
            Assert.Equal(initialStatus == "Resolved" ? stored.ResolvedUtc : null, stored.ResolvedUtc);
            Assert.Empty(dbContext.AdminAuditLogs);
        }
    }

    [Fact]
    public async Task ResolveReviewAsync_repeated_resolve_does_not_mutate_or_audit()
    {
        await using var dbContext = CreateDbContext();
        var reviewId = await SeedReviewAsync(dbContext, "Resolved", notes: "Final notes", resolvedUtc: DateTimeOffset.UtcNow.AddHours(-1));
        var service = new LoveBrackleyService(dbContext);

        await Assert.ThrowsAsync<ValidationException>(() => service.ResolveReviewAsync(reviewId, new ResolveReviewCaseRequest("Rewrite"), "actor-3", CancellationToken.None));

        var stored = await dbContext.AdminReviewCases.SingleAsync(candidate => candidate.Id == reviewId);
        Assert.Equal("Resolved", stored.Status);
        Assert.Equal("Final notes", stored.ResolutionNotes);
        Assert.NotNull(stored.ResolvedUtc);
        Assert.Empty(dbContext.AdminAuditLogs);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"love-brackley-review-{Guid.NewGuid():N}")
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<Guid> SeedReviewAsync(ApplicationDbContext dbContext, string status, string? notes = null, DateTimeOffset? resolvedUtc = null)
    {
        var reviewId = Guid.Parse("33333333-3333-3333-3333-333333333301");
        dbContext.AdminReviewCases.Add(new AdminReviewCaseEntity
        {
            Id = reviewId,
            CaseType = "BusinessApproval",
            Status = status,
            Priority = "High",
            Title = "Test Review Case",
            Description = "Test Description",
            ResolutionNotes = notes,
            CreatedUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ResolvedUtc = resolvedUtc
        });
        await dbContext.SaveChangesAsync();
        return reviewId;
    }

    private static void AssertReviewAuditState(string? json, string expectedStatus, string? expectedNotes, bool? expectedResolvedUtc)
    {
        Assert.False(string.IsNullOrWhiteSpace(json));
        using var document = JsonDocument.Parse(json!);
        var root = document.RootElement;
        Assert.Equal(expectedStatus, root.GetProperty("Status").GetString());

        if (expectedNotes is null)
        {
            Assert.Equal(JsonValueKind.Null, root.GetProperty("ResolutionNotes").ValueKind);
        }
        else
        {
            Assert.Equal(expectedNotes, root.GetProperty("ResolutionNotes").GetString());
        }

        if (expectedResolvedUtc is true)
        {
            Assert.Equal(JsonValueKind.String, root.GetProperty("ResolvedUtc").ValueKind);
        }
        else
        {
            Assert.Equal(JsonValueKind.Null, root.GetProperty("ResolvedUtc").ValueKind);
        }
    }
}
