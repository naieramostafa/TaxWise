using StreamlineTax.Application.Common.Interfaces;

namespace StreamlineTax.Infrastructure.Services;

public class TaxCalculationService : ITaxCalculationService
{
    public decimal CalculateTaxWithholding(decimal incomeAmount, decimal taxRate)
    {
        return Math.Round(incomeAmount * taxRate, 2);
    }

    public decimal EstimateAnnualTaxDue(decimal yearToDateIncome)
    {
        // Simplified progressive tax estimation
        if (yearToDateIncome <= 11000) return yearToDateIncome * 0.10m;
        if (yearToDateIncome <= 44725) return 1100 + (yearToDateIncome - 11000) * 0.12m;
        if (yearToDateIncome <= 95375) return 5147 + (yearToDateIncome - 44725) * 0.22m;
        if (yearToDateIncome <= 182100) return 16290 + (yearToDateIncome - 95375) * 0.24m;
        return 37104 + (yearToDateIncome - 182100) * 0.32m;
    }
}
