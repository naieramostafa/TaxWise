using StreamlineTax.Domain.Common;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Domain.Entities;

public class TaxPeriod : BaseEntity
{
    public Guid TaxAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TaxPeriodStatus Status { get; set; } = TaxPeriodStatus.Open;
    public decimal TotalIncome { get; set; }
    public decimal TotalTaxWithheld { get; set; }
    public decimal EstimatedTaxDue { get; set; }
    public decimal Balance { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public TaxAccount? TaxAccount { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
}