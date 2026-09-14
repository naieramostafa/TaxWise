using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;
using System.Security.Cryptography;

namespace StreamlineTax.Application.Receipts.Commands.UploadReceipt;

public class UploadReceiptCommandHandler(
    IApplicationDbContext context,
    IFileStorageService fileStorage,
    IBackgroundJobService jobService,
    INotificationService notificationService) : IRequestHandler<UploadReceiptCommand, UploadReceiptResponse>
{
    public async Task<UploadReceiptResponse> Handle(UploadReceiptCommand request, CancellationToken cancellationToken)
    {
        request.FileStream.Position = 0;
        var fileHash = await ComputeSha256Async(request.FileStream, cancellationToken);

        // Level 1 — exact file duplicate detection
        var existing = await context.Receipts
            .Where(r => r.UserId == request.UserId && r.FileHash == fileHash)
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            await notificationService.CreateAsync(
                request.UserId,
                NotificationType.DuplicateReceipt,
                "Duplicate receipt",
                $"You already uploaded {request.FileName}. This exact file was not processed again.",
                existing.Id,
                "Receipt",
                cancellationToken);

            return new UploadReceiptResponse(
                existing.Id,
                "Duplicate",
                IsDuplicate: true,
                DuplicateType: "ExactFile",
                ExistingReceiptId: existing.Id);
        }

        var storageKey = $"receipts/{request.UserId}/{Guid.NewGuid()}_{request.FileName}";

        var receipt = new Receipt
        {
            UserId = request.UserId,
            FileName = request.FileName,
            StorageKey = storageKey,
            ContentType = request.ContentType,
            FileSize = request.FileStream.Length,
            FileHash = fileHash,
            Status = ReceiptStatus.Pending
        };

        context.Receipts.Add(receipt);
        await context.SaveChangesAsync(cancellationToken);

        request.FileStream.Position = 0;
        await fileStorage.UploadAsync(request.FileStream, storageKey, request.ContentType, cancellationToken);

        jobService.EnqueueReceiptProcessing(receipt.Id);

        return new UploadReceiptResponse(receipt.Id, "Processing");
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}