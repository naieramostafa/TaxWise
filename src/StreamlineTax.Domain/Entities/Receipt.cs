using StreamlineTax.Domain.Common;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Domain.Entities;

public class Receipt : BaseEntity
{
    public Guid UserId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Pending;
    public string? ExtractedText { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public string? MerchantName { get; set; }
    public AppUser? User { get; set; }
}
