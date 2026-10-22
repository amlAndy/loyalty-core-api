using DotNetApiTemplate.Application.Features.Auth.Authorization;
using DotNetApiTemplate.Application.Features.LoveBrackley.Authorization;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/businesses")]
[Authorize(Policy = ActiveRolePolicies.Business)]
public sealed class BusinessesController : ControllerBase
{
    private readonly IBusinessAccessAuthorizationService _businessAccessAuthorizationService;
    private readonly IBusinessLoyaltyService _businessLoyaltyService;

    public BusinessesController(
        IBusinessAccessAuthorizationService businessAccessAuthorizationService,
        IBusinessLoyaltyService businessLoyaltyService)
    {
        _businessAccessAuthorizationService = businessAccessAuthorizationService;
        _businessLoyaltyService = businessLoyaltyService;
    }

    [HttpGet("{businessId:guid}/dashboard")]
    public async Task<ActionResult<DataResponse<BusinessDashboardDto>>> GetDashboard(Guid businessId, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.Dashboard, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.GetDashboardAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessDashboardDto>(result));
    }

    [HttpGet("{businessId:guid}/members")]
    public async Task<ActionResult<DataResponse<BusinessMemberSearchResponse>>> SearchMembers(Guid businessId, [FromQuery] string? searchTerm, [FromQuery] string? activityStatus, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.MemberSearch, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.SearchMembersAsync(businessId, searchTerm, activityStatus, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessMemberSearchResponse>(result));
    }

    [HttpGet("{businessId:guid}/members/{memberId:guid}")]
    public async Task<ActionResult<DataResponse<BusinessMemberDetailDto>>> GetMember(Guid businessId, Guid memberId, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.MemberDetails, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.GetMemberAsync(businessId, memberId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessMemberDetailDto>(result));
    }

    [HttpGet("{businessId:guid}/members/by-card/{cardCode}")]
    public async Task<ActionResult<DataResponse<BusinessMemberDetailDto>>> GetMemberByCard(Guid businessId, string cardCode, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.MemberCardLookup, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            var result = await _businessLoyaltyService.GetMemberByCardAsync(businessId, cardCode, cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<BusinessMemberDetailDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [HttpPost("{businessId:guid}/transactions/earn")]
    public async Task<ActionResult<DataResponse<LoyaltyTransactionDto>>> Earn(Guid businessId, [FromBody] EarnPointsRequest request, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.EarnPoints, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            var result = await _businessLoyaltyService.EarnAsync(businessId, request, cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<LoyaltyTransactionDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [HttpPost("{businessId:guid}/transactions/redeem")]
    public async Task<ActionResult<DataResponse<LoyaltyTransactionDto>>> Redeem(Guid businessId, [FromBody] RedeemPointsRequest request, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.RedeemPoints, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            var result = await _businessLoyaltyService.RedeemAsync(
                businessId,
                request,
                User.GetCurrentUserId(),
                User.GetActiveRoleId(),
                cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<LoyaltyTransactionDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [HttpGet("{businessId:guid}/transactions")]
    public async Task<ActionResult<DataResponse<BusinessTransactionSearchResponse>>> SearchTransactions(Guid businessId, [FromQuery] string? searchTerm, [FromQuery] string? dateRange, [FromQuery] string? transactionType, [FromQuery] Guid? staffMemberId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.Transactions, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.SearchTransactionsAsync(businessId, searchTerm, dateRange, transactionType, staffMemberId, status, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessTransactionSearchResponse>(result));
    }

    [HttpGet("{businessId:guid}/staff-access/overview")]
    public async Task<ActionResult<DataResponse<BusinessStaffAccessOverviewDto>>> GetStaffAccessOverview(Guid businessId, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.StaffAccessOverview, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.GetStaffAccessOverviewAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessStaffAccessOverviewDto>(result));
    }

    [HttpGet("{businessId:guid}/staff-access")]
    public async Task<ActionResult<DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>>> SearchStaffAccess(Guid businessId, [FromQuery] string? accessStatus, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.StaffAccessSearch, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.SearchStaffAccessAsync(businessId, accessStatus, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>(result));
    }

    [HttpPost("{businessId:guid}/staff-access")]
    public async Task<ActionResult<DataResponse<BusinessStaffAccessRecordDto>>> CreateStaffAccess(Guid businessId, [FromBody] CreateBusinessStaffAccessRequest request, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.StaffAccessCreate, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return Forbid();
    }

    [HttpPost("{businessId:guid}/staff-access/{staffAccessId:guid}/reset-pin")]
    public async Task<ActionResult<DataResponse<BusinessStaffAccessRecordDto>>> ResetStaffPin(Guid businessId, Guid staffAccessId, [FromBody] ResetStaffPinRequest request, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.StaffPinReset, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        return Forbid();
    }

    [HttpGet("{businessId:guid}/payouts/overview")]
    public async Task<ActionResult<DataResponse<BusinessPayoutOverviewDto>>> GetPayoutOverview(Guid businessId, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.PayoutOverview, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.GetPayoutOverviewAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessPayoutOverviewDto>(result));
    }

    [HttpGet("{businessId:guid}/payouts")]
    public async Task<ActionResult<DataResponse<PagedResponse<BusinessPayoutDto>>>> SearchPayouts(Guid businessId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.PayoutSearch, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.SearchPayoutsAsync(businessId, status, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<PagedResponse<BusinessPayoutDto>>(result));
    }

    [HttpGet("{businessId:guid}/settings")]
    public async Task<ActionResult<DataResponse<BusinessSettingsDto>>> GetSettings(Guid businessId, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.SettingsRead, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.GetSettingsAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessSettingsDto>(result));
    }

    [HttpPut("{businessId:guid}/settings")]
    public async Task<ActionResult<DataResponse<BusinessSettingsDto>>> UpdateSettings(Guid businessId, [FromBody] UpdateBusinessSettingsRequest request, CancellationToken cancellationToken)
    {
        var accessResult = await AuthorizeAsync(businessId, BusinessAccessOperation.SettingsUpdate, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var result = await _businessLoyaltyService.UpdateSettingsAsync(businessId, request, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessSettingsDto>(result));
    }

    private async Task<ActionResult?> AuthorizeAsync(Guid businessId, BusinessAccessOperation operation, CancellationToken cancellationToken)
    {
        var accessResult = await _businessAccessAuthorizationService.AuthorizeAsync(
            User.GetCurrentUserId(),
            User.GetActiveRoleId(),
            businessId,
            operation,
            cancellationToken);

        return accessResult.Decision switch
        {
            BusinessAccessDecision.Allowed => null,
            BusinessAccessDecision.NotFound => NotFound(),
            _ => Forbid()
        };
    }
}
