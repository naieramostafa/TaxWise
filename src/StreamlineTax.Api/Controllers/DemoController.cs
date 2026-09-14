using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamlineTax.Application.Demo.SeedDemoData;
using System.Security.Claims;

namespace StreamlineTax.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DemoController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var result = await mediator.Send(new SeedDemoDataCommand(UserId));
        return Ok(result);
    }
}
