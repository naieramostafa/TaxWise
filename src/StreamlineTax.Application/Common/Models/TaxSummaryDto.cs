namespace StreamlineTax.Application.Common.Models;

public record TaxSummaryDto(
    decimal TotalIncome,
    decimal TotalTaxWithheld,
    decimal EstimatedTaxDue,
    decimal Balance,
    decimal TaxWithholdingRate,
    int TransactionCount,
    decimal AvailableReserve,
    bool IsSurplus
);