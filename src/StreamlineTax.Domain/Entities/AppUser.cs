using Microsoft.AspNetCore.Identity;

namespace StreamlineTax.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal TaxWithholdingRate { get; set; } = 0.25m;
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Receipt> Receipts { get; set; } = [];
    public TaxAccount? TaxAccount { get; set; }
}
