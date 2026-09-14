namespace StreamlineTax.Application.Common.Interfaces;

public interface ITaxCalculationService
{
    decimal CalculateTaxWithholding(decimal incomeAmount, decimal taxRate);
    decimal EstimateAnnualTaxDue(decimal yearToDateIncome);
}
