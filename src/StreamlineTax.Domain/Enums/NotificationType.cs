namespace StreamlineTax.Domain.Enums;

public enum NotificationType
{
    ReceiptProcessed,
    ReceiptProcessingFailed,
    DuplicateReceipt,
    TaxDeadlineApproaching,
    TaxDeadlineOverdue,
    TaxDueChanged,
    WeeklyCategorization,
    TaxPeriodLocked
}