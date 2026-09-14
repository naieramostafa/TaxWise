using Microsoft.Extensions.Logging;
using StreamlineTax.Application.Common.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;
using Tesseract;

namespace StreamlineTax.Infrastructure.Services;

public class TesseractOcrService(ILogger<TesseractOcrService> logger) : IOcrService
{
    public async Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        var imageBytes = memoryStream.ToArray();

        string otsuText = "";
        float otsuConfidence = 0;
        using (var otsuPix = PreprocessOtsu(imageBytes))
        using (var otsuPage = engine.Process(otsuPix, PageSegMode.Auto))
        {
            otsuText = otsuPage.GetText();
            otsuConfidence = otsuPage.GetMeanConfidence();
        }
        logger.LogInformation("OCR (Otsu) confidence: {Confidence:P0}", otsuConfidence);
        logger.LogInformation("OCR (Otsu) raw text:\n{RawText}", otsuText);

        string grayText = "";
        float grayConfidence = 0;
        using (var grayPix = PreprocessGray(imageBytes))
        using (var grayPage = engine.Process(grayPix, PageSegMode.Auto))
        {
            grayText = grayPage.GetText();
            grayConfidence = grayPage.GetMeanConfidence();
        }
        logger.LogInformation("OCR (Gray) confidence: {Confidence:P0}", grayConfidence);
        logger.LogInformation("OCR (Gray) raw text:\n{RawText}", grayText);

        var combinedText = otsuText + "\n" + grayText;

        var amount = ExtractAmount(combinedText);
        var date = ExtractDate(combinedText);
        var merchant = ExtractMerchant(combinedText);

        logger.LogInformation("OCR extracted - Amount: {Amount}, Date: {Date}, Merchant: {Merchant}", amount, date, merchant);

        return new OcrResult(combinedText, amount, date, merchant);
    }

    private static Pix PreprocessOtsu(byte[] imageBytes)
    {
        using var original = Pix.LoadFromMemory(imageBytes);
        var scaled = original.Scale(3.0f, 3.0f);
        var gray = scaled.ConvertRGBToGray();
        return gray.BinarizeOtsuAdaptiveThreshold(100, 100, 0, 0, 0.0f);
    }

    private static Pix PreprocessGray(byte[] imageBytes)
    {
        using var original = Pix.LoadFromMemory(imageBytes);
        var scaled = original.Scale(3.0f, 3.0f);
        return scaled.ConvertRGBToGray();
    }

    private static decimal? ExtractAmount(string text)
    {
        var totalPattern = @"(?i)(?:grand\s*)?total\s*(?:item|qty)?\s*[:\s]*(?:Rp|IDR|[\$€£])?\s*(\d[\d.,]+)";
        var allTotals = Regex.Matches(text, totalPattern, RegexOptions.Multiline);
        foreach (Match m in allTotals)
        {
            var cleaned = m.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && result > 1000)
                return result;
        }

        var subtotalPattern = @"(?i)sub\s*total";
        decimal? subtotal = null;
        var subMatch = Regex.Match(text, subtotalPattern, RegexOptions.Multiline);
        if (subMatch.Success)
        {
            var before = text.Substring(0, subMatch.Index);
            var numsBefore = Regex.Matches(before, @"(\d[\d.,]+)");
            foreach (Match n in numsBefore.Cast<Match>().Reverse())
            {
                var cleaned = n.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v > 100)
                {
                    subtotal = v;
                    break;
                }
            }
        }

        decimal? tax = null;
        var taxPattern = @"(?i)tax\s*(?:resto|rate)?\s*\d*%?\s*[:\s]*(\d[\d.,]+)";
        var taxMatch = Regex.Match(text, taxPattern, RegexOptions.Multiline);
        if (taxMatch.Success)
        {
            var cleaned = taxMatch.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var tv) && tv > 0)
                tax = tv;
        }

        if (subtotal.HasValue && tax.HasValue)
            return subtotal.Value + tax.Value;

        if (subtotal.HasValue)
            return subtotal.Value;

        return null;
    }

    private static DateTime? ExtractDate(string text)
    {
        var textDatePattern = @"(?i)(Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+\d{1,2},?\s+\d{4}";
        var textMatch = Regex.Match(text, textDatePattern);
        if (textMatch.Success)
        {
            var cleaned = textMatch.Value.Replace(",", "");
            if (DateTime.TryParseExact(cleaned, "MMM d yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var textDate))
                return DateTime.SpecifyKind(textDate, DateTimeKind.Utc);
            if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out textDate))
                return DateTime.SpecifyKind(textDate, DateTimeKind.Utc);
        }

        var numericPatterns = new[]
        {
            @"\d{1,2}[/-]\d{1,2}[/-]\d{4}",
            @"\d{1,2}[/-]\d{1,2}[/-]\d{2}"
        };
        foreach (var pattern in numericPatterns)
        {
            var numericMatch = Regex.Match(text, pattern);
            if (numericMatch.Success && DateTime.TryParse(numericMatch.Value, out var numericDate))
                return DateTime.SpecifyKind(numericDate, DateTimeKind.Utc);
        }

        return null;
    }

    private static string? ExtractMerchant(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length >= 3 && Regex.IsMatch(trimmed, @"[A-Za-z]{2,}"))
            {
                var cleaned = Regex.Replace(trimmed, @"[^A-Za-z0-9\s&'-]+$", "").Trim();
                cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");
                return cleaned;
            }
        }
        return lines.Length > 0 ? lines[0].Trim() : null;
    }
}
