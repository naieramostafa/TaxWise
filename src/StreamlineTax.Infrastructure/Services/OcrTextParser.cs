using System.Globalization;
using System.Text.RegularExpressions;

namespace StreamlineTax.Infrastructure.Services;

public static class OcrTextParser
{
    public static decimal? ExtractAmount(string text)
    {
        var grandTotalPattern = @"(?i)grand\s*total\s*[:\s]*(?:Rp|IDR|[\$€£])?\s*(\d[\d.,]+)";
        var grandMatch = Regex.Match(text, grandTotalPattern, RegexOptions.Multiline);
        if (grandMatch.Success)
        {
            var cleaned = grandMatch.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && result > 1)
                return result;
        }

        var totalPattern = @"(?i)(?:grand\s*)?total\s*(?:item|qty)?\s*[:\s]*(?:Rp|IDR|[\$€£])?\s*(\d[\d.,]+)";
        var allTotals = Regex.Matches(text, totalPattern, RegexOptions.Multiline);
        foreach (Match m in allTotals)
        {
            var cleaned = m.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && result > 1000)
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

    public static DateTime? ExtractDate(string text)
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

    public static string? ExtractMerchant(string text)
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
