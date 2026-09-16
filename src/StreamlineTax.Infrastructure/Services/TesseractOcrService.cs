using Microsoft.Extensions.Logging;
using StreamlineTax.Application.Common.Interfaces;
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

        var amount = OcrTextParser.ExtractAmount(combinedText);
        var date = OcrTextParser.ExtractDate(combinedText);
        var merchant = OcrTextParser.ExtractMerchant(combinedText);

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
}
