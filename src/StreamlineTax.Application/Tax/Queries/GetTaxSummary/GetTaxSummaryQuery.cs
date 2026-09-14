using MediatR;
using StreamlineTax.Application.Common.Models;

namespace StreamlineTax.Application.Tax.Queries.GetTaxSummary;

public record GetTaxSummaryQuery(Guid UserId) : IRequest<TaxSummaryDto>;
