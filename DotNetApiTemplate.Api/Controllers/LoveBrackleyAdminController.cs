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

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("dashboard")]
    public async Task<ActionResult<DataResponse<AdminDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        return Ok(new DataResponse<AdminDashboardDto>(await _adminService.GetDashboardAsync(cancellationToken)));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("businesses")]
    public async Task<ActionResult<DataResponse<PagedResponse<AdminBusinessDto>>>> SearchBusinesses([FromQuery] string? searchTerm, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<AdminBusinessDto>>(await _adminService.SearchBusinessesAsync(searchTerm, status, page, pageSize, cancellationToken)));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("businesses")]
    public async Task<ActionResult<DataResponse<AdminBusinessDto>>> CreateBusiness([FromBody] CreateAdminBusinessRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateBusinessAsync(request, User.GetCurrentUserId(), cancellationToken);
        return Ok(new DataResponse<AdminBusinessDto>(result!));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("businesses/{businessId:guid}")]
    public async Task<ActionResult<DataResponse<BusinessSettingsDto>>> GetBusiness(Guid businessId, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetBusinessAsync(businessId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessSettingsDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPut("businesses/{businessId:guid}")]
    public async Task<ActionResult<DataResponse<BusinessSettingsDto>>> UpdateBusiness(Guid businessId, [FromBody] UpdateBusinessSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateBusinessAsync(businessId, request, User.GetCurrentUserId(), cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<BusinessSettingsDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("businesses/{businessId:guid}/approve")]
    public async Task<ActionResult<DataResponse<AdminBusinessDto>>> ApproveBusiness(Guid businessId, CancellationToken cancellationToken) => await SetBusinessStatus(businessId, "Active", "Approve", cancellationToken);

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("businesses/{businessId:guid}/suspend")]
    public async Task<ActionResult<DataResponse<AdminBusinessDto>>> SuspendBusiness(Guid businessId, CancellationToken cancellationToken) => await SetBusinessStatus(businessId, "Suspended", "Suspend", cancellationToken);

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("businesses/{businessId:guid}/reactivate")]
    public async Task<ActionResult<DataResponse<AdminBusinessDto>>> ReactivateBusiness(Guid businessId, CancellationToken cancellationToken) => await SetBusinessStatus(businessId, "Active", "Reactivate", cancellationToken);

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("members")]
    public async Task<ActionResult<DataResponse<PagedResponse<AdminMemberDto>>>> SearchMembers([FromQuery] string? searchTerm, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<AdminMemberDto>>(await _adminService.SearchMembersAsync(searchTerm, status, page, pageSize, cancellationToken)));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("members")]
    public async Task<ActionResult<DataResponse<AdminMemberDto>>> CreateMember([FromBody] CreateAdminMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateMemberAsync(request, cancellationToken);
        return Ok(new DataResponse<AdminMemberDto>(result!));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("members/{memberId:guid}")]
    public async Task<ActionResult<DataResponse<AdminMemberDetailDto>>> GetMember(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetMemberAsync(memberId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminMemberDetailDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPut("members/{memberId:guid}")]
    public async Task<ActionResult<DataResponse<AdminMemberDetailDto>>> UpdateMember(Guid memberId, [FromBody] UpdateMemberSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateMemberAsync(memberId, request, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminMemberDetailDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("members/{memberId:guid}/lock")]
    public async Task<ActionResult<DataResponse<AdminMemberDto>>> LockMember(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await _adminService.SetMemberStatusAsync(memberId, "Suspended", cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminMemberDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("members/{memberId:guid}/unlock")]
    public async Task<ActionResult<DataResponse<AdminMemberDto>>> UnlockMember(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await _adminService.SetMemberStatusAsync(memberId, "Active", cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminMemberDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
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

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("reviews")]
    public async Task<ActionResult<DataResponse<PagedResponse<AdminReviewCaseDto>>>> SearchReviews([FromQuery] string? caseType, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<AdminReviewCaseDto>>(await _adminService.SearchReviewsAsync(caseType, status, priority, page, pageSize, cancellationToken)));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("reviews/{caseId:guid}")]
    public async Task<ActionResult<DataResponse<AdminReviewCaseDto>>> GetReview(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetReviewAsync(caseId, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<AdminReviewCaseDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("reviews/{caseId:guid}/resolve")]
    [ProducesResponseType(typeof(DataResponse<AdminReviewCaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DataResponse<AdminReviewCaseDto>>> ResolveReview(Guid caseId, [FromBody] ResolveReviewCaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminService.ResolveReviewAsync(caseId, request, User.GetCurrentUserId(), cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<AdminReviewCaseDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("reviews/{caseId:guid}/escalate")]
    [ProducesResponseType(typeof(DataResponse<AdminReviewCaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DataResponse<AdminReviewCaseDto>>> EscalateReview(Guid caseId, [FromBody] EscalateReviewCaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminService.EscalateReviewAsync(caseId, request, User.GetCurrentUserId(), cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<AdminReviewCaseDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("transactions")]
    public async Task<ActionResult<DataResponse<PagedResponse<LoyaltyTransactionDto>>>> SearchTransactions([FromQuery] Guid? businessId, [FromQuery] Guid? memberId, [FromQuery] string? transactionType, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<LoyaltyTransactionDto>>(await _adminService.SearchTransactionsAsync(businessId, memberId, transactionType, status, page, pageSize, cancellationToken)));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("access")]
    public async Task<ActionResult<DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>>> SearchAccess([FromQuery] Guid? businessId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<BusinessStaffAccessRecordDto>>(await _adminService.SearchAccessAsync(businessId, status, page, pageSize, cancellationToken)));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPost("access")]
    public async Task<ActionResult<DataResponse<BusinessStaffAccessRecordDto>>> CreateAccessProvisioning([FromBody] CreateBusinessStaffAccessProvisioningRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminService.CreateAccessProvisioningAsync(request, User.GetCurrentUserId(), cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<BusinessStaffAccessRecordDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
    [HttpPut("access/{staffAccessId:guid}/provisioning")]
    public async Task<ActionResult<DataResponse<BusinessStaffAccessRecordDto>>> UpdateAccessProvisioning(Guid staffAccessId, [FromBody] UpdateBusinessStaffAccessProvisioningRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminService.UpdateAccessProvisioningAsync(staffAccessId, request, User.GetCurrentUserId(), cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<BusinessStaffAccessRecordDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("payouts")]
    public async Task<ActionResult<DataResponse<PagedResponse<BusinessPayoutDto>>>> SearchPayouts([FromQuery] Guid? businessId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(new DataResponse<PagedResponse<BusinessPayoutDto>>(await _adminService.SearchPayoutsAsync(businessId, status, page, pageSize, cancellationToken)));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminView)]
    [HttpGet("settings/points-rule")]
    public async Task<ActionResult<DataResponse<PointsRuleDto>>> GetPointsRule(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetPointsRuleAsync(cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<PointsRuleDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AdminConfigure)]
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

    [Authorize(Policy = AdminPermissionPolicies.AuditView)]
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
