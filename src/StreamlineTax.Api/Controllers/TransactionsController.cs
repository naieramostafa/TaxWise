using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamlineTax.Application.Transactions.Commands.CategorizeTransaction;
using StreamlineTax.Application.Transactions.Commands.DeleteTransaction;
using StreamlineTax.Application.Transactions.Commands.UpdateTransaction;
using StreamlineTax.Application.Transactions.Queries.GetTransactions;
using System.Security.Claims;

namespace StreamlineTax.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? month, [FromQuery] string? category)
    {
        var query = new GetTransactionsQuery(UserId, year, month, category);
        var transactions = await mediator.Send(query);
        return Ok(transactions);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionCommand command)
    {
        if (id != command.TransactionId)
            return BadRequest("Transaction ID mismatch");

        await mediator.Send(command with { UserId = UserId });
        return NoContent();
    }

    [HttpPut("{id}/category")]
    public async Task<IActionResult> Categorize(Guid id, [FromBody] CategorizeTransactionCommand command)
    {
        if (id != command.TransactionId)
            return BadRequest("Transaction ID mismatch");

        await mediator.Send(command with { UserId = UserId });
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteTransactionCommand(id, UserId);
        await mediator.Send(command);
        return NoContent();
    }
}