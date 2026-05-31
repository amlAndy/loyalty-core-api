using DotNetApiTemplate.Application.Features.Admin.Authorization;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminAccess)]
public sealed class LoveBrackleyAdminController : ControllerBase
{
    private readonly IAdminLoveBrackleyService _adminService;

    public LoveBrackleyAdminController(IAdminLoveBrackleyService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DataResponse<AdminDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        return Ok(new DataResponse<AdminDashboardDto>(await _adminService.GetDashboardAsync(cancellationToken)));
    }

    [HttpGet("businesses")]
    public async Task<ActionResult<DataResponse<PagedResponse<AdminBusinessDto>>>> SearchBusinesses([FromQuery] string? searchTerm, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<AdminBusinessDto>>(await _adminService.SearchBusinessesAsync(searchTerm, status, page, pageSize, cancellationToken)));
    }

    [HttpPost("businesses")]
    public async Task<ActionResult<DataResponse<AdminBusinessDto>>> CreateBusiness([FromBody] CreateAdminBusinessRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateBusinessAsync(request, User.GetCurrentUserId(), cancellationToken);
        return Ok(new DataResponse<AdminBusinessDto>(result!));
    }

    [HttpGet("businesses/{businessId:guid}")]
    public async Task<ActionResult<DataResponse<BusinessSettingsDto>>> GetBusiness(Guid businessId, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetBusinessAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessSettingsDto>(result));
    }

    [HttpPut("businesses/{businessId:guid}")]
    public async Task<ActionResult<DataResponse<BusinessSettingsDto>>> UpdateBusiness(Guid businessId, [FromBody] UpdateBusinessSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateBusinessAsync(businessId, request, User.GetCurrentUserId(), cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessSettingsDto>(result));
    }

    [HttpPost("businesses/{businessId:guid}/approve")]
    public async Task<ActionResult<DataResponse<AdminBusinessDto>>> ApproveBusiness(Guid businessId, CancellationToken cancellationToken) => await SetBusinessStatus(businessId, "Active", "Approve", cancellationToken);

    [HttpPost("businesses/{businessId:guid}/suspend")]
    public async Task<ActionResult<DataResponse<AdminBusinessDto>>> SuspendBusiness(Guid businessId, CancellationToken cancellationToken) => await SetBusinessStatus(businessId, "Suspended", "Suspend", cancellationToken);

    [HttpPost("businesses/{businessId:guid}/reactivate")]
    public async Task<ActionResult<DataResponse<AdminBusinessDto>>> ReactivateBusiness(Guid businessId, CancellationToken cancellationToken) => await SetBusinessStatus(businessId, "Active", "Reactivate", cancellationToken);

    [HttpGet("members")]
    public async Task<ActionResult<DataResponse<PagedResponse<AdminMemberDto>>>> SearchMembers([FromQuery] string? searchTerm, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<AdminMemberDto>>(await _adminService.SearchMembersAsync(searchTerm, status, page, pageSize, cancellationToken)));
    }

    [HttpPost("members")]
    public async Task<ActionResult<DataResponse<AdminMemberDto>>> CreateMember([FromBody] CreateAdminMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateMemberAsync(request, cancellationToken);
        return Ok(new DataResponse<AdminMemberDto>(result!));
    }

    [HttpGet("members/{memberId:guid}")]
    public async Task<ActionResult<DataResponse<AdminMemberDto>>> GetMember(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetMemberAsync(memberId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminMemberDto>(result));
    }

    [HttpPut("members/{memberId:guid}")]
    public async Task<ActionResult<DataResponse<AdminMemberDto>>> UpdateMember(Guid memberId, [FromBody] UpdateMemberSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateMemberAsync(memberId, request, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminMemberDto>(result));
    }

    [HttpPost("members/{memberId:guid}/lock")]
    public async Task<ActionResult<DataResponse<AdminMemberDto>>> LockMember(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await _adminService.SetMemberStatusAsync(memberId, "Suspended", cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminMemberDto>(result));
    }

    [HttpPost("members/{memberId:guid}/unlock")]
    public async Task<ActionResult<DataResponse<AdminMemberDto>>> UnlockMember(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await _adminService.SetMemberStatusAsync(memberId, "Active", cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminMemberDto>(result));
    }

    [HttpPost("members/{memberId:guid}/adjust-points")]
    public async Task<ActionResult<DataResponse<LoyaltyTransactionDto>>> AdjustPoints(Guid memberId, [FromBody] AdjustPointsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminService.AdjustPointsAsync(memberId, request, User.GetCurrentUserId(), cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<LoyaltyTransactionDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<DataResponse<PagedResponse<AdminReviewCaseDto>>>> SearchReviews([FromQuery] string? caseType, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<AdminReviewCaseDto>>(await _adminService.SearchReviewsAsync(caseType, status, priority, page, pageSize, cancellationToken)));
    }

    [HttpGet("reviews/{caseId:guid}")]
    public async Task<ActionResult<DataResponse<AdminReviewCaseDto>>> GetReview(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetReviewAsync(caseId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminReviewCaseDto>(result));
    }

    [HttpPost("reviews/{caseId:guid}/resolve")]
    public async Task<ActionResult<DataResponse<AdminReviewCaseDto>>> ResolveReview(Guid caseId, [FromBody] ResolveReviewCaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.ResolveReviewAsync(caseId, request, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminReviewCaseDto>(result));
    }

    [HttpPost("reviews/{caseId:guid}/escalate")]
    public async Task<ActionResult<DataResponse<AdminReviewCaseDto>>> EscalateReview(Guid caseId, [FromBody] EscalateReviewCaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.EscalateReviewAsync(caseId, request, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminReviewCaseDto>(result));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<DataResponse<PagedResponse<LoyaltyTransactionDto>>>> SearchTransactions([FromQuery] Guid? businessId, [FromQuery] Guid? memberId, [FromQuery] string? transactionType, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<LoyaltyTransactionDto>>(await _adminService.SearchTransactionsAsync(businessId, memberId, transactionType, status, page, pageSize, cancellationToken)));
    }

    [HttpGet("access")]
    public async Task<ActionResult<DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>>> SearchAccess([FromQuery] Guid? businessId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>(await _adminService.SearchAccessAsync(businessId, status, page, pageSize, cancellationToken)));
    }

    [HttpGet("payouts")]
    public async Task<ActionResult<DataResponse<PagedResponse<BusinessPayoutDto>>>> SearchPayouts([FromQuery] Guid? businessId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<BusinessPayoutDto>>(await _adminService.SearchPayoutsAsync(businessId, status, page, pageSize, cancellationToken)));
    }

    [HttpGet("settings/points-rule")]
    public async Task<ActionResult<DataResponse<PointsRuleDto>>> GetPointsRule(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetPointsRuleAsync(cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<PointsRuleDto>(result));
    }

    [HttpPut("settings/points-rule")]
    public async Task<ActionResult<DataResponse<PointsRuleDto>>> UpdatePointsRule([FromBody] UpdatePointsRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminService.UpdatePointsRuleAsync(request, User.GetCurrentUserId(), cancellationToken);
            return Ok(new DataResponse<PointsRuleDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [HttpGet("love-brackley-audit")]
    public async Task<ActionResult<DataResponse<PagedResponse<AdminAuditLogDto>>>> SearchAudit([FromQuery] string? entityType, [FromQuery] string? entityId, [FromQuery] string? actorUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<AdminAuditLogDto>>(await _adminService.SearchAuditAsync(entityType, entityId, actorUserId, page, pageSize, cancellationToken)));
    }

    private async Task<ActionResult<DataResponse<AdminBusinessDto>>> SetBusinessStatus(Guid businessId, string status, string action, CancellationToken cancellationToken)
    {
        var result = await _adminService.SetBusinessStatusAsync(businessId, status, action, User.GetCurrentUserId(), cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminBusinessDto>(result));
    }
}
