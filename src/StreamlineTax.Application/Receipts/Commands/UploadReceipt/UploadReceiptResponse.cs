namespace StreamlineTax.Application.Receipts.Commands.UploadReceipt;

public record UploadReceiptResponse(
    Guid ReceiptId,
    string Status,
    bool IsDuplicate = false,
    string? DuplicateType = null,
    Guid? ExistingReceiptId = null);