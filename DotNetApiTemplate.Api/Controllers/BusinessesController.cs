using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/businesses")]
[Authorize]
public sealed class BusinessesController : ControllerBase
{
    private readonly IBusinessLoyaltyService _businessLoyaltyService;

    public BusinessesController(IBusinessLoyaltyService businessLoyaltyService)
    {
        _businessLoyaltyService = businessLoyaltyService;
    }

    [HttpGet("{businessId:guid}/dashboard")]
    public async Task<ActionResult<DataResponse<BusinessDashboardDto>>> GetDashboard(Guid businessId, CancellationToken cancellationToken)
    {
        var result = await _businessLoyaltyService.GetDashboardAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessDashboardDto>(result));
    }

    [HttpGet("{businessId:guid}/members")]
    public async Task<ActionResult<DataResponse<BusinessMemberSearchResponse>>> SearchMembers(Guid businessId, [FromQuery] string? searchTerm, [FromQuery] string? activityStatus, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _businessLoyaltyService.SearchMembersAsync(businessId, searchTerm, activityStatus, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessMemberSearchResponse>(result));
    }

    [HttpGet("{businessId:guid}/members/{memberId:guid}")]
    public async Task<ActionResult<DataResponse<BusinessMemberDetailDto>>> GetMember(Guid businessId, Guid memberId, CancellationToken cancellationToken)
    {
        var result = await _businessLoyaltyService.GetMemberAsync(businessId, memberId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessMemberDetailDto>(result));
    }

    [HttpGet("{businessId:guid}/members/by-card/{cardCode}")]
    public async Task<ActionResult<DataResponse<BusinessMemberDetailDto>>> GetMemberByCard(Guid businessId, string cardCode, CancellationToken cancellationToken)
    {
        var result = await _businessLoyaltyService.GetMemberByCardAsync(businessId, cardCode, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessMemberDetailDto>(result));
    }

    [HttpPost("{businessId:guid}/transactions/earn")]
    public async Task<ActionResult<DataResponse<LoyaltyTransactionDto>>> Earn(Guid businessId, [FromBody] EarnPointsRequest request, CancellationToken cancellationToken)
    {
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
        try
        {
            var result = await _businessLoyaltyService.RedeemAsync(businessId, request, cancellationToken);
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
        var result = await _businessLoyaltyService.SearchTransactionsAsync(businessId, searchTerm, dateRange, transactionType, staffMemberId, status, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessTransactionSearchResponse>(result));
    }

    [HttpGet("{businessId:guid}/staff-access/overview")]
    public async Task<ActionResult<DataResponse<BusinessStaffAccessOverviewDto>>> GetStaffAccessOverview(Guid businessId, CancellationToken cancellationToken)
    {
        var result = await _businessLoyaltyService.GetStaffAccessOverviewAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessStaffAccessOverviewDto>(result));
    }

    [HttpGet("{businessId:guid}/staff-access")]
    public async Task<ActionResult<DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>>> SearchStaffAccess(Guid businessId, [FromQuery] string? pinStatus, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _businessLoyaltyService.SearchStaffAccessAsync(businessId, pinStatus, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>(result));
    }

    [HttpPost("{businessId:guid}/staff-access")]
    public async Task<ActionResult<DataResponse<BusinessStaffAccessRecordDto>>> CreateStaffAccess(Guid businessId, [FromBody] CreateBusinessStaffAccessRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _businessLoyaltyService.CreateStaffAccessAsync(businessId, request, cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<BusinessStaffAccessRecordDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [HttpPost("{businessId:guid}/staff-access/{staffAccessId:guid}/reset-pin")]
    public async Task<ActionResult<DataResponse<BusinessStaffAccessRecordDto>>> ResetStaffPin(Guid businessId, Guid staffAccessId, [FromBody] ResetStaffPinRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _businessLoyaltyService.ResetStaffPinAsync(businessId, staffAccessId, request, User.GetCurrentUserId(), cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<BusinessStaffAccessRecordDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [HttpGet("{businessId:guid}/payouts/overview")]
    public async Task<ActionResult<DataResponse<BusinessPayoutOverviewDto>>> GetPayoutOverview(Guid businessId, CancellationToken cancellationToken)
    {
        var result = await _businessLoyaltyService.GetPayoutOverviewAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessPayoutOverviewDto>(result));
    }

    [HttpGet("{businessId:guid}/payouts")]
    public async Task<ActionResult<DataResponse<PagedResponse<BusinessPayoutDto>>>> SearchPayouts(Guid businessId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _businessLoyaltyService.SearchPayoutsAsync(businessId, status, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<PagedResponse<BusinessPayoutDto>>(result));
    }

    [HttpGet("{businessId:guid}/settings")]
    public async Task<ActionResult<DataResponse<BusinessSettingsDto>>> GetSettings(Guid businessId, CancellationToken cancellationToken)
    {
        var result = await _businessLoyaltyService.GetSettingsAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessSettingsDto>(result));
    }

    [HttpPut("{businessId:guid}/settings")]
    public async Task<ActionResult<DataResponse<BusinessSettingsDto>>> UpdateSettings(Guid businessId, [FromBody] UpdateBusinessSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await _businessLoyaltyService.UpdateSettingsAsync(businessId, request, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessSettingsDto>(result));
    }
}
