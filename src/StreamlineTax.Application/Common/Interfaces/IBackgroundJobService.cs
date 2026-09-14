namespace StreamlineTax.Application.Common.Interfaces;

public interface IBackgroundJobService
{
    void EnqueueReceiptProcessing(Guid receiptId);
}
