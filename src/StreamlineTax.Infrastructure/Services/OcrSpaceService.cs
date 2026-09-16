using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreamlineTax.Application.Common.Interfaces;
using System.Text.Json;

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

        var amount = OcrTextParser.ExtractAmount(extractedText);
        var date = OcrTextParser.ExtractDate(extractedText);
        var merchant = OcrTextParser.ExtractMerchant(extractedText);

        _logger.LogInformation("OCR.space extracted - Amount: {Amount}, Date: {Date}, Merchant: {Merchant}", amount, date, merchant);

        return new OcrResult(extractedText, amount, date, merchant);
    }
}
