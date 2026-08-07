using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiTemplate.Api.Controllers;

[ApiController]
[Route("api/business-categories")]
public sealed class BusinessCategoriesController : ControllerBase
{
    private readonly ILoveBrackleyLookupService _lookupService;

    public BusinessCategoriesController(ILoveBrackleyLookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<ActionResult<DataResponse<IReadOnlyList<BusinessCategoryDto>>>> Get(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetBusinessCategoriesAsync(cancellationToken);
        return Ok(new DataResponse<IReadOnlyList<BusinessCategoryDto>>(result));
    }
}
