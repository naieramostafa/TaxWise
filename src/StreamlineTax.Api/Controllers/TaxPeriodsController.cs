using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Application.Transactions.Queries.GetTransactions;
using MediatR;
using System.Security.Claims;

namespace StreamlineTax.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaxPeriodsController(
    ITaxPeriodService taxPeriodService,
    IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var periods = await taxPeriodService.GetForUserAsync(UserId);
        var comparison = await taxPeriodService.GetComparisonAsync(UserId);
        return Ok(new { periods, comparison });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var period = await taxPeriodService.GetByIdAsync(UserId, id);
        if (period is null)
            return NotFound(new { error = $"Tax period {id} not found" });

        var transactions = await mediator.Send(new GetTransactionsQuery(UserId, period.StartDate.Year, null, null));
        var periodTransactions = transactions
            .Where(t => t.TransactionDate >= period.StartDate && t.TransactionDate <= period.EndDate)
            .Select(t => new
            {
                t.Id,
                t.Amount,
                t.Description,
                category = t.Category.ToString(),
                t.TransactionDate,
                t.TaxWithheld
            })
            .ToList();

        return Ok(new { period, transactions = periodTransactions });
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        var period = await taxPeriodService.CloseAsync(UserId, id);
        return Ok(period);
    }

    [HttpPost("{id}/lock")]
    public async Task<IActionResult> Lock(Guid id)
    {
        var period = await taxPeriodService.LockAsync(UserId, id);
        return Ok(period);
    }
}