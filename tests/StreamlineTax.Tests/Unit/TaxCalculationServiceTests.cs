using FluentAssertions;
using StreamlineTax.Infrastructure.Services;

namespace StreamlineTax.Tests.Unit;

public class TaxCalculationServiceTests
{
    private readonly TaxCalculationService _sut = new();

    [Theory]
    [InlineData(10000, 0.25, 2500)]
    [InlineData(0, 0.25, 0)]
    [InlineData(100000, 0.3, 30000)]
    public void CalculateTaxWithholding_ReturnsCorrectResult(decimal income, decimal rate, decimal expected)
    {
        var result = _sut.CalculateTaxWithholding(income, rate);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10000, 1000)]     // 10% bracket
    [InlineData(30000, 3380)]     // 12% bracket (1100 + 19000*0.12)
    [InlineData(50000, 6307.50)]  // 22% bracket (5147 + 5275*0.22)
    [InlineData(100000, 17400)]   // 24% bracket (16290 + 4625*0.24)
    public void EstimateAnnualTaxDue_ReturnsProgressiveTax(decimal income, decimal expected)
    {
        var result = _sut.EstimateAnnualTaxDue(income);
        result.Should().Be(expected);
    }

    [Fact]
    public void EstimateAnnualTaxDue_ZeroIncome_ReturnsZero()
    {
        var result = _sut.EstimateAnnualTaxDue(0);
        result.Should().Be(0);
    }
}
