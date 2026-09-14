using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamlineTax.Application.Receipts.Commands.UploadReceipt;
using StreamlineTax.Application.Receipts.Queries.GetReceipts;
using System.Security.Claims;

namespace StreamlineTax.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReceiptsController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided");

        using var stream = file.OpenReadStream();
        var command = new UploadReceiptCommand(UserId, file.FileName, file.ContentType, stream);
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetReceiptsQuery(UserId);
        var receipts = await mediator.Send(query);
        return Ok(receipts);
    }
}
