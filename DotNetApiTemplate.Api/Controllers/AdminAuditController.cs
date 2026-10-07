using AutoMapper;
using DotNetApiTemplate.Api.Contracts.Requests.Admin;
using DotNetApiTemplate.Api.Contracts.Responses.Admin;
using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;
using DotNetApiTemplate.Application.Features.Admin.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminAccess)]
public sealed class AdminAuditController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;

    public AdminAuditController(IMapper mapper, IAuditService auditService)
    {
        _mapper = mapper;
        _auditService = auditService;
    }

    [Authorize(Policy = AdminPermissionPolicies.AuditView)]
    [HttpGet]
    [ProducesResponseType(typeof(AuditSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditSearchResponseDto>> Search(
        [FromQuery] AuditSearchCriteriaDto request,
        CancellationToken cancellationToken)
    {
        var criteria = _mapper.Map<AuditSearchCriteria>(request);
        var result = await _auditService.SearchAsync(criteria, cancellationToken);

        return Ok(_mapper.Map<AuditSearchResponseDto>(result));
    }

    [Authorize(Policy = AdminPermissionPolicies.AuditView)]
    [HttpGet("filters")]
    [ProducesResponseType(typeof(AuditFilterOptionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditFilterOptionsDto>> GetFilterOptions(CancellationToken cancellationToken)
    {
        var result = await _auditService.GetFilterOptionsAsync(cancellationToken);

        return Ok(_mapper.Map<AuditFilterOptionsDto>(result));
    }
}
