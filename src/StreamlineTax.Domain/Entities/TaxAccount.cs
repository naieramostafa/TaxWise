using StreamlineTax.Domain.Common;

namespace StreamlineTax.Domain.Entities;

public class TaxAccount : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalTaxWithheld { get; set; }
    public decimal EstimatedTaxDue { get; set; }
    public decimal Balance { get; set; }
    public AppUser? User { get; set; }
    public ICollection<TaxPeriod> TaxPeriods { get; set; } = [];
}
