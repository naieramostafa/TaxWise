using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamlineTax.Application.Tax.Queries.GetTaxSummary;
using System.Security.Claims;

namespace StreamlineTax.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaxController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var query = new GetTaxSummaryQuery(UserId);
        var summary = await mediator.Send(query);
        return Ok(summary);
    }
}
