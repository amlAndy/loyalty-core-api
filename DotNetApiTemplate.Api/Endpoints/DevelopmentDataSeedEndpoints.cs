using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Api.Endpoints;

public static class DevelopmentDataSeedEndpoints
{
    private static readonly TimeZoneInfo LondonTimeZone = ResolveLondonTimeZone();

    public static IEndpointRouteBuilder MapDevelopmentDataSeedEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dev/data")
            .WithTags("Development");

        group.MapPost("/green-pantry-dashboard-activity", SeedGreenPantryDashboardActivityAsync)
            .WithName("SeedGreenPantryDashboardActivity")
            .WithSummary("Seeds development-only Green Pantry dashboard activity")
            .WithDescription("Creates idempotent dashboard activity for Green Pantry using domain transaction services. Available only in Development.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> SeedGreenPantryDashboardActivityAsync(
        ApplicationDbContext dbContext,
        IBusinessLoyaltyService businessLoyaltyService,
        CancellationToken cancellationToken)
    {
        var business = await dbContext.BusinessProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.BrandReference == "LB-BIZ-2008", cancellationToken);

        if (business is null)
        {
            return Results.NotFound(new ProblemDetails { Detail = "Green Pantry business profile was not found." });
        }

        var businessLocationId = await dbContext.BusinessLocations
            .AsNoTracking()
            .Where(candidate => candidate.BusinessProfileId == business.Id && candidate.Status == "Active")
            .OrderBy(candidate => candidate.CreatedUtc)
            .Select(candidate => (Guid?)candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var staffAccessId = await dbContext.BusinessStaffAccesses
            .AsNoTracking()
            .Where(candidate => candidate.BusinessProfileId == business.Id && candidate.PinStatus == "Active")
            .OrderBy(candidate => candidate.CreatedUtc)
            .Select(candidate => (Guid?)candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var ownerRoleId = await dbContext.UserRoles
            .AsNoTracking()
            .Where(candidate => candidate.UserId == business.OwnerUserId)
            .Join(
                dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.RoleId, role.Name })
            .Where(candidate => candidate.Name == ApplicationRoleNames.BusinessOwner)
            .Select(candidate => candidate.RoleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(ownerRoleId))
        {
            return Results.BadRequest(new ProblemDetails { Detail = "Green Pantry owner role assignment was not found." });
        }

        var memberIds = await dbContext.MemberProfiles
            .AsNoTracking()
            .OrderBy(candidate => candidate.CreatedUtc)
            .Take(3)
            .Select(candidate => candidate.Id)
            .ToListAsync(cancellationToken);

        if (memberIds.Count < 3)
        {
            return Results.BadRequest(new ProblemDetails { Detail = "Not enough seeded members were found to create dashboard transactions." });
        }

        var londonNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, LondonTimeZone);
        var londonToday = londonNow.Date;

        var operations = new[]
        {
            new SeedOperation("DEV-SEED-GREEN-PANTRY-TODAY-EARN-1", "Earn", memberIds[0], PurchaseAmount: 48.75m, Points: null, LondonDate: londonToday, LondonHour: 10, LondonMinute: 15),
            new SeedOperation("DEV-SEED-GREEN-PANTRY-TODAY-REDEEM-1", "Redeem", memberIds[0], PurchaseAmount: 32.40m, Points: 30, LondonDate: londonToday, LondonHour: 13, LondonMinute: 40),
            new SeedOperation("DEV-SEED-GREEN-PANTRY-TODAY-EARN-2", "Earn", memberIds[1], PurchaseAmount: 21.30m, Points: null, LondonDate: londonToday, LondonHour: 16, LondonMinute: 20),
            new SeedOperation("DEV-SEED-GREEN-PANTRY-OLDER-EARN-1", "Earn", memberIds[2], PurchaseAmount: 15.00m, Points: null, LondonDate: londonToday.AddDays(-3), LondonHour: 11, LondonMinute: 5),
            new SeedOperation("DEV-SEED-GREEN-PANTRY-OLDER-REDEEM-1", "Redeem", memberIds[1], PurchaseAmount: 27.50m, Points: 10, LondonDate: londonToday.AddDays(-2), LondonHour: 15, LondonMinute: 10),
        };

        var created = new List<object>();
        var skipped = new List<object>();

        foreach (var operation in operations)
        {
            var existing = await dbContext.LoyaltyTransactions
                .AsNoTracking()
                .Where(candidate => candidate.BusinessProfileId == business.Id && candidate.Notes == operation.Note)
                .Select(candidate => new { candidate.Id, candidate.Reference, candidate.CreatedUtc })
                .SingleOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                skipped.Add(new
                {
                    operation.Note,
                    existing.Id,
                    existing.Reference,
                    existing.CreatedUtc
                });
                continue;
            }

            LoyaltyTransactionDto? createdTransaction = operation.Type == "Earn"
                ? await businessLoyaltyService.EarnAsync(
                    business.Id,
                    new EarnPointsRequest(operation.MemberId, staffAccessId, businessLocationId, operation.PurchaseAmount, operation.Note),
                    cancellationToken)
                : await businessLoyaltyService.RedeemAsync(
                    business.Id,
                    new RedeemPointsRequest(operation.MemberId, businessLocationId, operation.Points ?? 0, operation.PurchaseAmount, operation.Note),
                    business.OwnerUserId,
                    ownerRoleId,
                    cancellationToken);

            if (createdTransaction is null)
            {
                return Results.BadRequest(new ProblemDetails { Detail = $"Failed to create seeded {operation.Type.ToLowerInvariant()} transaction '{operation.Note}'." });
            }

            var transaction = await dbContext.LoyaltyTransactions.SingleAsync(candidate => candidate.Id == createdTransaction.Id, cancellationToken);
            var targetUtc = ConvertLondonToUtc(operation.LondonDate, operation.LondonHour, operation.LondonMinute);
            transaction.CreatedUtc = targetUtc;
            transaction.CompletedUtc = targetUtc;
            await dbContext.SaveChangesAsync(cancellationToken);

            created.Add(new
            {
                operation.Note,
                transaction.Id,
                transaction.Reference,
                transaction.TransactionType,
                transaction.Points,
                transaction.PurchaseAmount,
                transaction.RedeemedValue,
                transaction.CreatedUtc
            });
        }

        var (todayStartUtc, tomorrowStartUtc) = GetLondonDayRangeUtc(DateTimeOffset.UtcNow);

        var completedToday = await dbContext.LoyaltyTransactions
            .AsNoTracking()
            .Where(candidate =>
                candidate.BusinessProfileId == business.Id
                && candidate.Status == "Completed"
                && candidate.CreatedUtc >= todayStartUtc
                && candidate.CreatedUtc < tomorrowStartUtc)
            .OrderByDescending(candidate => candidate.CreatedUtc)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Reference,
                candidate.TransactionType,
                candidate.Points,
                candidate.RedeemedValue,
                candidate.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            businessId = business.Id,
            businessName = business.Name,
            londonDate = londonToday.ToString("yyyy-MM-dd"),
            windowStartUtc = todayStartUtc,
            windowEndUtc = tomorrowStartUtc,
            created,
            skipped,
            totals = new
            {
                pointsIssuedToday = completedToday.Where(candidate => candidate.TransactionType == "Earn").Sum(candidate => candidate.Points),
                pointsRedeemedToday = completedToday.Where(candidate => candidate.TransactionType == "Redeem").Sum(candidate => candidate.Points),
                transactionsToday = completedToday.Count,
            },
            recentCompletedToday = completedToday.Take(5).ToList(),
        });
    }

    private static DateTimeOffset ConvertLondonToUtc(DateTime londonDate, int londonHour, int londonMinute)
    {
        var localDateTime = new DateTime(
            londonDate.Year,
            londonDate.Month,
            londonDate.Day,
            londonHour,
            londonMinute,
            0,
            DateTimeKind.Unspecified);

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, LondonTimeZone);
        return new DateTimeOffset(utcDateTime);
    }

    private static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetLondonDayRangeUtc(DateTimeOffset utcNow)
    {
        var londonNow = TimeZoneInfo.ConvertTime(utcNow, LondonTimeZone);
        var londonDate = londonNow.Date;

        var localMidnightStart = new DateTime(londonDate.Year, londonDate.Month, londonDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localMidnightEnd = localMidnightStart.AddDays(1);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localMidnightStart, LondonTimeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localMidnightEnd, LondonTimeZone);

        return (new DateTimeOffset(startUtc), new DateTimeOffset(endUtc));
    }

    private static TimeZoneInfo ResolveLondonTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
    }

    private sealed record SeedOperation(
        string Note,
        string Type,
        Guid MemberId,
        decimal PurchaseAmount,
        int? Points,
        DateTime LondonDate,
        int LondonHour,
        int LondonMinute);
}
