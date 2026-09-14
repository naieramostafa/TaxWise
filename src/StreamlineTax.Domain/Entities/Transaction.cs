using StreamlineTax.Domain.Common;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionCategory Category { get; set; } = TransactionCategory.Uncategorized;
    public DateTime TransactionDate { get; set; }
    public decimal TaxWithheld { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? TaxPeriodId { get; set; }
    public AppUser? User { get; set; }
    public Receipt? Receipt { get; set; }
    public TaxPeriod? TaxPeriod { get; set; }
}
