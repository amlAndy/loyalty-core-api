using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/towns")]
public sealed class TownsController : ControllerBase
{
    private readonly ILoveBrackleyLookupService _lookupService;

    public TownsController(ILoveBrackleyLookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<ActionResult<DataResponse<IReadOnlyList<TownSummaryDto>>>> Get(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetTownsAsync(cancellationToken);
        return Ok(new DataResponse<IReadOnlyList<TownSummaryDto>>(result));
    }
}
