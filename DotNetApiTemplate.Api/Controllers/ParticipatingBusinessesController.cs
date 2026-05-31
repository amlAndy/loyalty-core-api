using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/participating-businesses")]
public sealed class ParticipatingBusinessesController : ControllerBase
{
    private readonly IParticipatingBusinessService _participatingBusinessService;

    public ParticipatingBusinessesController(IParticipatingBusinessService participatingBusinessService)
    {
        _participatingBusinessService = participatingBusinessService;
    }

    [HttpGet]
    public async Task<ActionResult<DataResponse<PagedResponse<ParticipatingBusinessSummaryDto>>>> Search(
        [FromQuery] string? category,
        [FromQuery] string? searchTerm,
        [FromQuery] bool? featured,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _participatingBusinessService.SearchAsync(category, searchTerm, featured, page, pageSize, cancellationToken);
        return Ok(new DataResponse<PagedResponse<ParticipatingBusinessSummaryDto>>(result));
    }
}
