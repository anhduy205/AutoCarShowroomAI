using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class AiCarAdvisorChatService : IAiChatService
{
    private static readonly Regex IdRegex = new(
        @"(?:^|[^\d])(?:id|ma)?\s*#?\s*(\d{1,9})(?!\d)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RangeRegex = new(
        @"\btu\s+(?<a>[\d.,]+\s*(ty|trieu|tr|m|k)?)\s+den\s+(?<b>[\d.,]+\s*(ty|trieu|tr|m|k)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MoneyRegex = new(
        @"(?<n>[\d.,]+)\s*(?<u>ty|trieu|tr|m|k)?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IInventoryManagementService _inventory;
    private readonly OpenAiChatService _openAi;

    public AiCarAdvisorChatService(IInventoryManagementService inventory, OpenAiChatService openAi)
    {
        _inventory = inventory;
        _openAi = openAi;
    }

    public async Task<AiChatResult> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new FriendlyOperationException("Noi dung tin nhan khong duoc de trong.");
        }

        userMessage = userMessage.Trim();
        var normalized = NormalizeForHeuristics(userMessage);

        if (WantsCompare(normalized))
        {
            var compareIds = TryExtractCompareIds(normalized);
            if (compareIds.Count >= 2)
            {
                var left = await _inventory.GetCarDetailsAsync(compareIds[0], cancellationToken);
                var right = await _inventory.GetCarDetailsAsync(compareIds[1], cancellationToken);

                if (left is not null && right is not null)
                {
                    var prompt = BuildComparePrompt(userMessage, left, right);
                    return await _openAi.GetReplyAsync(prompt, cancellationToken);
                }
            }

            // Fallback: user asked to compare but didn't provide IDs.
            // Try retrieving top 2 cars relevant to the query.
            var fallback = await _inventory.GetCarsForChatAsync(
                new CarChatSearchRequest { Query = userMessage, Take = 2 },
                cancellationToken);

            if (fallback.Count >= 2)
            {
                var left = await _inventory.GetCarDetailsAsync(fallback[0].Id, cancellationToken);
                var right = await _inventory.GetCarDetailsAsync(fallback[1].Id, cancellationToken);

                if (left is not null && right is not null)
                {
                    var prompt = BuildComparePrompt(userMessage, left, right);
                    return await _openAi.GetReplyAsync(prompt, cancellationToken);
                }
            }
        }

        var search = BuildSearchRequest(userMessage, normalized);
        var cars = await _inventory.GetCarsForChatAsync(search, cancellationToken);

        var augmentedPrompt = BuildAdvisorPrompt(userMessage, cars);
        return await _openAi.GetReplyAsync(augmentedPrompt, cancellationToken);
    }

    private static bool WantsCompare(string normalizedMessage)
        => normalizedMessage.Contains("so sanh", StringComparison.Ordinal) ||
           normalizedMessage.Contains("compare", StringComparison.Ordinal) ||
           normalizedMessage.Contains(" vs ", StringComparison.Ordinal) ||
           normalizedMessage.Contains("vs.", StringComparison.Ordinal) ||
           normalizedMessage.StartsWith("vs ", StringComparison.Ordinal);

    private static List<int> TryExtractCompareIds(string normalizedMessage)
    {
        var ids = new List<int>();
        foreach (Match match in IdRegex.Matches(normalizedMessage))
        {
            if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            if (id <= 0)
            {
                continue;
            }

            if (!ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static CarChatSearchRequest BuildSearchRequest(string userMessage, string normalizedMessage)
    {
        var request = new CarChatSearchRequest
        {
            Query = userMessage,
            Take = 8
        };

        // Very lightweight heuristic parsing (Vietnamese, diacritics-stripped).
        if (normalizedMessage.Contains("suv", StringComparison.Ordinal))
        {
            request = request with { Type = "SUV" };
        }
        else if (normalizedMessage.Contains("sedan", StringComparison.Ordinal))
        {
            request = request with { Type = "Sedan" };
        }
        else if (normalizedMessage.Contains("hatchback", StringComparison.Ordinal))
        {
            request = request with { Type = "Hatchback" };
        }
        else if (normalizedMessage.Contains("pickup", StringComparison.Ordinal) ||
                 normalizedMessage.Contains("ban tai", StringComparison.Ordinal))
        {
            request = request with { Type = "Pickup" };
        }

        var (minPrice, maxPrice) = TryExtractPriceRange(normalizedMessage);
        if (minPrice is not null || maxPrice is not null)
        {
            request = request with { MinPrice = minPrice, MaxPrice = maxPrice };
        }

        var years = TryExtractYearRange(normalizedMessage);
        if (years.yearFrom is not null || years.yearTo is not null)
        {
            request = request with { YearFrom = years.yearFrom, YearTo = years.yearTo };
        }

        return request;
    }

    private static (decimal? min, decimal? max) TryExtractPriceRange(string normalizedMessage)
    {
        // "tu A den B"
        var range = RangeRegex.Match(normalizedMessage);
        if (range.Success)
        {
            var a = TryParseMoneyToVnd(range.Groups["a"].Value);
            var b = TryParseMoneyToVnd(range.Groups["b"].Value);
            if (a is not null && b is not null)
            {
                return a <= b ? (a, b) : (b, a);
            }
        }

        // single amount with context "duoi"/"tren"
        var matches = MoneyRegex.Matches(normalizedMessage);
        if (matches.Count == 0)
        {
            return (null, null);
        }

        var first = TryParseMoneyToVnd(matches[0].Value);
        if (first is null)
        {
            return (null, null);
        }

        if (normalizedMessage.Contains("duoi", StringComparison.Ordinal) ||
            normalizedMessage.Contains("<=", StringComparison.Ordinal) ||
            normalizedMessage.Contains("toi da", StringComparison.Ordinal) ||
            normalizedMessage.Contains("max", StringComparison.Ordinal))
        {
            return (null, first);
        }

        if (normalizedMessage.Contains("tren", StringComparison.Ordinal) ||
            normalizedMessage.Contains(">=", StringComparison.Ordinal) ||
            normalizedMessage.Contains("toi thieu", StringComparison.Ordinal) ||
            normalizedMessage.Contains("min", StringComparison.Ordinal))
        {
            return (first, null);
        }

        // No operator -> treat as max budget if looks like "ngan sach X"
        if (normalizedMessage.Contains("ngan sach", StringComparison.Ordinal) ||
            normalizedMessage.Contains("budget", StringComparison.Ordinal))
        {
            return (null, first);
        }

        return (null, null);
    }

    private static (int? yearFrom, int? yearTo) TryExtractYearRange(string normalizedMessage)
    {
        var years = Regex.Matches(normalizedMessage, @"\b(19\d{2}|20\d{2})\b");
        if (years.Count == 0)
        {
            return (null, null);
        }

        var values = years
            .Select(m => int.TryParse(m.Groups[1].Value, out var y) ? y : 0)
            .Where(y => y is >= 1900 and <= 2100)
            .Distinct()
            .Order()
            .ToList();

        if (values.Count == 1)
        {
            return (values[0], values[0]);
        }

        return (values.First(), values.Last());
    }

    private static decimal? TryParseMoneyToVnd(string input)
    {
        var match = MoneyRegex.Match(input);
        if (!match.Success)
        {
            return null;
        }

        var rawNumber = match.Groups["n"].Value.Replace(",", ".", StringComparison.Ordinal);
        if (!decimal.TryParse(rawNumber, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        var unit = match.Groups["u"].Value.ToLowerInvariant();
        var multiplier = unit switch
        {
            "ty" => 1_000_000_000m,
            "trieu" or "tr" or "m" => 1_000_000m,
            "k" => 1_000m,
            _ => 1m
        };

        // If number is small and no unit, assume millions for common VN car pricing prompts (e.g. "800")
        if (multiplier == 1m && number is >= 50 and <= 5000)
        {
            multiplier = 1_000_000m;
        }

        return Math.Max(0, number * multiplier);
    }

    private static string BuildAdvisorPrompt(string userMessage, IReadOnlyList<CarChatCatalogItem> cars)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Yeu cau khach hang:");
        sb.AppendLine(userMessage);
        sb.AppendLine();
        sb.AppendLine("Ban la AI tu van showroom. Chi su dung thong tin xe trong danh muc duoi day (khong tu che).");
        sb.AppendLine("Neu khong du du lieu, hay hoi toi da 2 cau de lam ro (ngan sach, loai xe, so cho, muc dich).");
        sb.AppendLine("Tra loi bang tieng Viet, goi y 2-4 mau xe phu hop, kem ly do ngan gon va tom tat gia/nam/loai.");
        sb.AppendLine();

        if (cars.Count == 0)
        {
            sb.AppendLine("Danh muc xe hien co: (trong)");
            return sb.ToString();
        }

        sb.AppendLine("Danh muc xe hien co:");
        foreach (var car in cars)
        {
            var statusLabel = car.Status switch
            {
                CarStatusCatalog.Promotion => "Khuyen mai",
                _ => "Con hang"
            };

            sb.Append("- ");
            sb.Append($"#{car.Id} {car.BrandName} {car.Name}");
            sb.Append($" | {car.Type ?? "-"}");
            sb.Append($" | Nam: {(car.Year?.ToString(CultureInfo.InvariantCulture) ?? "-")}");
            sb.Append($" | Mau: {car.Color ?? "-"}");
            sb.Append($" | Gia: {car.Price.ToString("N0", CultureInfo.InvariantCulture)} VND");
            sb.Append($" | Ton: {car.StockQuantity}");
            sb.Append($" | {statusLabel}");

            var spec = NormalizeSpec(car.Specifications);
            if (!string.IsNullOrWhiteSpace(spec))
            {
                sb.Append($" | Specs: {spec}");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("Neu can so sanh, hay goi y cach so sanh va hoac de xuat 2 lua chon gan nhat.");
        return sb.ToString();
    }

    private static string BuildComparePrompt(string userMessage, CarDetailsViewModel left, CarDetailsViewModel right)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Nguoi dung muon so sanh 2 xe:");
        sb.AppendLine(userMessage);
        sb.AppendLine();
        sb.AppendLine("Chi su dung thong tin xe duoi day (khong tu che). Tra loi bang tieng Viet.");
        sb.AppendLine("Hay so sanh theo: gia, nam, loai, mau, ton kho, va thong so ky thuat neu co. Ket luan nen chon xe nao theo tung nhu cau.");
        sb.AppendLine();
        sb.AppendLine("XE A:");
        AppendCarDetails(sb, left);
        sb.AppendLine();
        sb.AppendLine("XE B:");
        AppendCarDetails(sb, right);
        return sb.ToString();
    }

    private static void AppendCarDetails(StringBuilder sb, CarDetailsViewModel car)
    {
        sb.AppendLine($"Id: {car.Id}");
        sb.AppendLine($"Ten: {car.BrandName} {car.Name}");
        sb.AppendLine($"Loai: {car.Type ?? "-"}");
        sb.AppendLine($"Nam: {(car.Year?.ToString(CultureInfo.InvariantCulture) ?? "-")}");
        sb.AppendLine($"Mau: {car.Color ?? "-"}");
        sb.AppendLine($"Trang thai: {car.Status}");
        sb.AppendLine($"Gia: {car.Price.ToString("N0", CultureInfo.InvariantCulture)} VND");
        sb.AppendLine($"Ton kho: {car.StockQuantity}");

        var spec = NormalizeSpec(car.Specifications);
        if (!string.IsNullOrWhiteSpace(spec))
        {
            sb.AppendLine($"Thong so: {spec}");
        }
    }

    private static string? NormalizeSpec(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return null;
        }

        var normalized = spec.Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= 220 ? normalized : normalized[..220] + "...";
    }

    private static string NormalizeForHeuristics(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var formD = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
    }
}

