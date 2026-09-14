using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;
using Transaction = StreamlineTax.Domain.Entities.Transaction;

namespace StreamlineTax.Infrastructure.Services.BackgroundJobs;

public class ReceiptProcessingJob(
    IApplicationDbContext context,
    IFileStorageService fileStorage,
    IOcrService ocrService,
    ITaxCalculationService taxCalculation,
    ITaxPeriodService taxPeriodService,
    INotificationService notificationService,
    ILogger<ReceiptProcessingJob> logger)
{
    public async Task ProcessAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await context.Receipts.FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken);
        if (receipt is null)
        {
            logger.LogWarning("Receipt {ReceiptId} not found", receiptId);
            return;
        }

        try
        {
            receipt.Status = ReceiptStatus.Processing;
            await context.SaveChangesAsync(cancellationToken);

            using var stream = await fileStorage.DownloadAsync(receipt.StorageKey, cancellationToken);
            var ocrResult = await ocrService.ExtractTextAsync(stream, cancellationToken);

            receipt.ExtractedText = ocrResult.RawText;
            receipt.Amount = ocrResult.Amount;
            receipt.ReceiptDate = ocrResult.Date;
            receipt.MerchantName = ocrResult.MerchantName;
            receipt.Status = ReceiptStatus.Processed;
            receipt.UpdatedAt = DateTime.UtcNow;

            if (ocrResult.Amount.HasValue)
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == receipt.UserId, cancellationToken);
                if (user is not null)
                {
                    var taxWithheld = taxCalculation.CalculateTaxWithholding(ocrResult.Amount.Value, user.TaxWithholdingRate);

                    // Assign the transaction to the correct tax period based on its date
                    var taxPeriod = await taxPeriodService.GetPeriodForDateAsync(
                        receipt.UserId, ocrResult.Date ?? DateTime.UtcNow, cancellationToken)
                        ?? throw new InvalidOperationException("Failed to resolve tax period");

                    var transaction = new Transaction
                    {
                        UserId = receipt.UserId,
                        Amount = ocrResult.Amount.Value,
                        Description = $"Receipt: {receipt.MerchantName ?? receipt.FileName}",
                        TransactionDate = ocrResult.Date ?? DateTime.UtcNow,
                        TaxWithheld = taxWithheld,
                        ReceiptId = receipt.Id,
                        TaxPeriodId = taxPeriod.Id
                    };

                    context.Transactions.Add(transaction);

                    var taxAccount = await context.TaxAccounts
                        .FirstOrDefaultAsync(t => t.UserId == receipt.UserId, cancellationToken);
                    if (taxAccount is null)
                    {
                        taxAccount = new TaxAccount { UserId = receipt.UserId };
                        context.TaxAccounts.Add(taxAccount);
                    }

                    taxAccount.TotalIncome += ocrResult.Amount.Value;
                    taxAccount.TotalTaxWithheld += taxWithheld;
                    taxAccount.EstimatedTaxDue = taxCalculation.EstimateAnnualTaxDue(taxAccount.TotalIncome);
                    taxAccount.Balance = taxAccount.TotalTaxWithheld - taxAccount.EstimatedTaxDue;
                    taxAccount.UpdatedAt = DateTime.UtcNow;

                    await context.SaveChangesAsync(cancellationToken);
                    await taxPeriodService.RecalculateAsync(receipt.UserId, taxPeriod.Id, cancellationToken);

                    // Level 2 — OCR/data duplicate detection (possible duplicate, not hard-blocked)
                    if (!string.IsNullOrWhiteSpace(receipt.MerchantName) && ocrResult.Amount.HasValue && ocrResult.Date.HasValue)
                    {
                        var possibleDuplicate = await context.Transactions
                            .AnyAsync(t =>
                                t.UserId == receipt.UserId &&
                                t.Id != transaction.Id &&
                                t.Amount == ocrResult.Amount.Value &&
                                t.TransactionDate.Date == ocrResult.Date.Value.Date &&
                                t.Description.Contains(receipt.MerchantName!),
                                cancellationToken);

                        if (possibleDuplicate)
                        {
                            await notificationService.CreateAsync(
                                receipt.UserId,
                                NotificationType.DuplicateReceipt,
                                "Possible duplicate receipt",
                                $"A receipt from {receipt.MerchantName} for {ocrResult.Amount.Value:C} on {ocrResult.Date.Value:d} may already exist.",
                                transaction.Id,
                                "Transaction",
                                cancellationToken);
                        }
                    }

                    await notificationService.CreateAsync(
                        receipt.UserId,
                        NotificationType.ReceiptProcessed,
                        "Receipt processed",
                        $"Your receipt from {receipt.MerchantName ?? receipt.FileName} was successfully processed.",
                        transaction.Id,
                        "Transaction",
                        cancellationToken);
                }
            }
            else
            {
                await context.SaveChangesAsync(cancellationToken);
                await notificationService.CreateAsync(
                    receipt.UserId,
                    NotificationType.ReceiptProcessed,
                    "Receipt processed",
                    $"Your receipt {receipt.FileName} was processed, but no amount could be extracted.",
                    receipt.Id,
                    "Receipt",
                    cancellationToken);
            }

            logger.LogInformation("Receipt {ReceiptId} processed successfully", receiptId);
        }
        catch (Exception ex)
        {
            receipt.Status = ReceiptStatus.Failed;
            receipt.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            await notificationService.CreateAsync(
                receipt.UserId,
                NotificationType.ReceiptProcessingFailed,
                "Receipt processing failed",
                $"We couldn't process {receipt.FileName}. Please try uploading it again.",
                receipt.Id,
                "Receipt",
                cancellationToken);

            logger.LogError(ex, "Failed to process receipt {ReceiptId}", receiptId);
        }
    }
}