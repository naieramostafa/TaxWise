namespace StreamlineTax.Application.Common.Interfaces;

public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default);
}

public record OcrResult(string RawText, decimal? Amount, DateTime? Date, string? MerchantName);
