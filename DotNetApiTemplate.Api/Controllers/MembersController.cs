using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/members")]
[Authorize]
public sealed class MembersController : ControllerBase
{
    private readonly IMemberLoyaltyService _memberLoyaltyService;

    public MembersController(IMemberLoyaltyService memberLoyaltyService)
    {
        _memberLoyaltyService = memberLoyaltyService;
    }

    [HttpGet("me/dashboard")]
    public async Task<ActionResult<DataResponse<MemberDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _memberLoyaltyService.GetDashboardAsync(User.GetCurrentUserId(), cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<MemberDashboardDto>(result));
    }

    [HttpGet("me/wallet")]
    public async Task<ActionResult<DataResponse<MemberWalletDto>>> GetWallet(CancellationToken cancellationToken)
    {
        var result = await _memberLoyaltyService.GetWalletAsync(User.GetCurrentUserId(), cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<MemberWalletDto>(result));
    }

    [HttpGet("me/activity")]
    public async Task<ActionResult<DataResponse<MemberActivityResponse>>> GetActivity([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _memberLoyaltyService.GetActivityAsync(User.GetCurrentUserId(), page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<MemberActivityResponse>(result));
    }

    [HttpGet("me/card")]
    public async Task<ActionResult<DataResponse<MemberCardDto>>> GetCard(CancellationToken cancellationToken)
    {
        var result = await _memberLoyaltyService.GetCardAsync(User.GetCurrentUserId(), cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<MemberCardDto>(result));
    }

    [HttpGet("me/settings")]
    public async Task<ActionResult<DataResponse<MemberSettingsDto>>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await _memberLoyaltyService.GetSettingsAsync(User.GetCurrentUserId(), cancellationToken);
        return result is null ? NotFound() : Ok(new DataResponse<MemberSettingsDto>(result));
    }

    [HttpPut("me/settings")]
    public async Task<ActionResult<DataResponse<MemberSettingsDto>>> UpdateSettings([FromBody] UpdateMemberSettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _memberLoyaltyService.UpdateSettingsAsync(User.GetCurrentUserId(), request, cancellationToken);
            return result is null ? NotFound() : Ok(new DataResponse<MemberSettingsDto>(result));
        }
        catch (ValidationException exception)
        {
            return LoveBrackleyControllerHelpers.CreateValidationProblem(exception);
        }
    }
}
