using Hangfire;
using StreamlineTax.Application.Common.Interfaces;

namespace StreamlineTax.Infrastructure.Services.BackgroundJobs;

public class BackgroundJobService(IBackgroundJobClient jobClient) : IBackgroundJobService
{
    public void EnqueueReceiptProcessing(Guid receiptId)
    {
        jobClient.Enqueue<ReceiptProcessingJob>(j => j.ProcessAsync(receiptId, CancellationToken.None));
    }
}
