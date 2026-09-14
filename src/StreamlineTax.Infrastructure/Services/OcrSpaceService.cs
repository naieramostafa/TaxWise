using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreamlineTax.Application.Common.Interfaces;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StreamlineTax.Infrastructure.Services;

public class OcrSpaceService : IOcrService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OcrSpaceService> _logger;
    private readonly string _apiKey;

    public OcrSpaceService(HttpClient httpClient, ILogger<OcrSpaceService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OCR:ApiKey"]
            ?? configuration["OcrSpace:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OCRSPACE_API_KEY")
            ?? throw new InvalidOperationException("OCR.space API key not configured. Set OCR:ApiKey or OCRSPACE_API_KEY.");
    }

    public async Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);
        var dataUri = $"data:image/jpeg;base64,{base64Image}";

        var content = new MultipartFormDataContent
        {
            { new StringContent(dataUri), "base64Image" },
            { new StringContent("2"), "OCREngine" },
            { new StringContent("eng"), "language" },
            { new StringContent("true"), "isOverlayRequired" },
            { new StringContent("true"), "isTable" },
            { new StringContent("true"), "scale" },
            { new StringContent("true"), "detectOrientation" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ocr.space/parse/image")
        {
            Content = content
        };
        request.Headers.Add("apikey", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation("OCR.space response status: {Status}", response.StatusCode);
        _logger.LogInformation("OCR.space full response: {Json}", json);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OCR.space API error: {Json}", json);
            return new OcrResult("", null, null, null);
        }

        var parsedResult = JsonSerializer.Deserialize<JsonElement>(json);

        var extractedText = "";
        if (parsedResult.TryGetProperty("ParsedResults", out var results) && results.GetArrayLength() > 0)
        {
            extractedText = results[0].GetProperty("ParsedText").GetString() ?? "";
        }

        _logger.LogInformation("OCR.space raw text:\n{RawText}", extractedText);

        var amount = ExtractAmount(extractedText);
        var date = ExtractDate(extractedText);
        var merchant = ExtractMerchant(extractedText);

        _logger.LogInformation("OCR.space extracted - Amount: {Amount}, Date: {Date}, Merchant: {Merchant}", amount, date, merchant);

        return new OcrResult(extractedText, amount, date, merchant);
    }

    private static decimal? ExtractAmount(string text)
    {
        var grandTotalPattern = @"(?i)grand\s*total\s*[:\s]*[\$€£]?\s*(\d[\d.,]+)";
        var grandMatch = Regex.Match(text, grandTotalPattern, RegexOptions.Multiline);
        if (grandMatch.Success)
        {
            var cleaned = grandMatch.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && result > 1)
                return result;
        }

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (Regex.IsMatch(line, @"(?i)^total\s*$"))
            {
                for (int j = i - 1; j >= Math.Max(0, i - 5); j--)
                {
                    var prevLine = lines[j].Trim();
                    var nums = Regex.Matches(prevLine, @"(\d[\d.,]+)");
                    foreach (Match n in nums)
                    {
                        var cleaned = n.Groups[1].Value.Replace(",", "");
                        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v > 100)
                            return v;
                    }
                }
            }
        }

        var subtotalPattern = @"(?i)sub\s*total\s*[:\s]*[\$€£]?\s*(\d[\d.,]+)";
        var subMatch = Regex.Match(text, subtotalPattern, RegexOptions.Multiline);
        if (subMatch.Success)
        {
            var cleaned = subMatch.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && result > 0)
                return result;
        }

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
            if (Regex.IsMatch(line, @"(?i)(total\s|subtotal|tax|tip|service|thank|come\s+back|grand|order\s*time|party|server|table|dine|printed)"))
                break;

            var segments = line.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (Regex.IsMatch(trimmed, @"(?i)(street|ave|road|rd|blvd|dr|way|lane|ln|st\b|WA\s+\d{5}|Tel[\.\:]|\d{3}[\s.-]\d{3}[\s.-]\d{4}|www\.|http|welcome|order\s*#|\d{5,}|T\.\d)"))
                    continue;
                if (Regex.IsMatch(trimmed, @"^\d"))
                    continue;
                if (trimmed.Length >= 3 && Regex.IsMatch(trimmed, @"[A-Za-z]{3,}"))
                {
                    var cleaned = Regex.Replace(trimmed, @"[^A-Za-z0-9\s&'.-]+$", "").Trim();
                    cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");
                    if (cleaned.Length >= 3)
                        return cleaned;
                }
            }
        }
        return lines.Length > 0 ? lines[0].Trim() : null;
    }
}
