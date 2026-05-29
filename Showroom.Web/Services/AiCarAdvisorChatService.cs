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
    private readonly ITextGenerationService _textGeneration;

    public AiCarAdvisorChatService(IInventoryManagementService inventory, ITextGenerationService textGeneration)
    {
        _inventory = inventory;
        _textGeneration = textGeneration;
    }

    public async Task<AiChatResult> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new FriendlyOperationException("Nội dung tin nhắn không được để trống.");
        }

        userMessage = userMessage.Trim();
        var normalized = NormalizeForHeuristics(userMessage);

        var explicitId = TryExtractSingleCarId(normalized);
        if (explicitId is not null && !WantsCompare(normalized))
        {
            var details = await _inventory.GetCarDetailsAsync(explicitId.Value, cancellationToken);
            if (details is not null && LooksLikeDetailsQuestion(normalized))
            {
                return BuildDeterministicCarAnswer(details);
            }
        }

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
                    return await _textGeneration.GenerateAsync(prompt, cancellationToken);
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
                    return await _textGeneration.GenerateAsync(prompt, cancellationToken);
                }
            }
        }

        if (LooksLikeInventoryCountQuestion(normalized))
        {
            var all = await _inventory.GetCarsAsync(cancellationToken);
            var available = await _inventory.GetCarsForChatAsync(new CarChatSearchRequest { Take = 1000 }, cancellationToken);
            return BuildInventoryCountAnswer(userMessage, all, available);
        }

        var search = BuildSearchRequest(userMessage, normalized);
        var cars = await _inventory.GetCarsForChatAsync(search, cancellationToken);

        if (cars.Count == 0)
        {
            // If no matches, provide nearest grounded options instead of repeatedly asking for info already provided.
            var nearest = await _inventory.GetCarsForChatAsync(
                new CarChatSearchRequest
                {
                    BrandId = search.BrandId,
                    Type = search.Type,
                    YearFrom = search.YearFrom,
                    YearTo = search.YearTo,
                    Query = null,
                    Take = 3
                },
                cancellationToken);

            if (nearest.Count == 0)
            {
                nearest = await _inventory.GetCarsForChatAsync(new CarChatSearchRequest { Take = 3 }, cancellationToken);
            }

            return BuildDeterministicAdvisorNoMatchAnswer(userMessage, search, nearest);
        }

        // Deterministic response to guarantee answers are grounded in database content.
        // This prevents the upstream LLM from hallucinating cars that do not exist.
        return BuildDeterministicAdvisorAnswer(userMessage, search, cars);
    }

    private static int? TryExtractSingleCarId(string normalizedMessage)
    {
        var match = IdRegex.Match(normalizedMessage);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return null;
        }

        return id > 0 ? id : null;
    }

    private static bool LooksLikeDetailsQuestion(string normalizedMessage)
    {
        // Heuristic: question about a specific car details should be answered deterministically from DB.
        return normalizedMessage.Contains("gia", StringComparison.Ordinal) ||
               normalizedMessage.Contains("bao nhieu", StringComparison.Ordinal) ||
               normalizedMessage.Contains("thong so", StringComparison.Ordinal) ||
               normalizedMessage.Contains("spec", StringComparison.Ordinal) ||
               normalizedMessage.Contains("mau", StringComparison.Ordinal) ||
               normalizedMessage.Contains("nam", StringComparison.Ordinal) ||
               normalizedMessage.Contains("ton", StringComparison.Ordinal) ||
               normalizedMessage.Contains("con hang", StringComparison.Ordinal) ||
               normalizedMessage.Contains("trang thai", StringComparison.Ordinal) ||
               normalizedMessage.Contains("khuyen mai", StringComparison.Ordinal) ||
               normalizedMessage.Contains("mo ta", StringComparison.Ordinal) ||
               normalizedMessage.Contains("chi tiet", StringComparison.Ordinal);
    }

    private static bool LooksLikeInventoryCountQuestion(string normalizedMessage)
        => normalizedMessage.Contains("bao nhieu xe", StringComparison.Ordinal) ||
           normalizedMessage.Contains("co bao nhieu xe", StringComparison.Ordinal) ||
           normalizedMessage.Contains("tong so xe", StringComparison.Ordinal) ||
           normalizedMessage.Contains("hien co bao nhieu", StringComparison.Ordinal);

    private static AiChatResult BuildDeterministicCarAnswer(CarDetailsViewModel car)
    {
        var statusLabel = car.Status switch
        {
            CarStatusCatalog.InStock => "Còn hàng",
            CarStatusCatalog.Promotion => "Khuyến mãi",
            CarStatusCatalog.Sold => "Đã bán",
            _ => car.Status
        };

        var sb = new StringBuilder();
        sb.AppendLine($"Thông tin xe #{car.Id} ({car.BrandName} {car.Name})");
        sb.AppendLine($"Link: /cars/{car.Id}");
        sb.AppendLine($"Giá: {car.Price.ToString("N0", CultureInfo.InvariantCulture)} VND");
        sb.AppendLine($"Trạng thái: {statusLabel}");
        sb.AppendLine($"Tồn kho: {car.StockQuantity}");
        sb.AppendLine($"Loại: {car.Type ?? "-"}");
        sb.AppendLine($"Năm: {(car.Year?.ToString(CultureInfo.InvariantCulture) ?? "-")}");
        sb.AppendLine($"Màu: {car.Color ?? "-"}");

        if (!string.IsNullOrWhiteSpace(car.Specifications))
        {
            sb.AppendLine();
            sb.AppendLine("Thông số kỹ thuật:");
            sb.AppendLine(NormalizeEscapedNewLines(car.Specifications).Trim());
        }

        return new AiChatResult(sb.ToString().Trim(), Provider: "Database");
    }

    private static AiChatResult BuildDeterministicAdvisorAnswer(
        string userMessage,
        CarChatSearchRequest search,
        IReadOnlyList<CarChatCatalogItem> cars)
    {
        var sb = new StringBuilder();
        // Không nhắc lại nội dung người dùng vừa nhập.

        if (cars.Count == 0)
        {
            sb.AppendLine("Hiện tại không có xe phù hợp trong danh mục.");
            sb.AppendLine("Bạn cho mình biết thêm (tối đa 2 ý):");
            sb.AppendLine("- Ngân sách tối đa (VND) hoặc khoảng giá?");
            sb.AppendLine("- Cần loại xe/số chỗ/mục đích sử dụng (đi phố/đi du lịch/gia đình)?");
            return new AiChatResult(sb.ToString().Trim(), Provider: "Database");
        }

        var take = Math.Min(4, cars.Count);
        sb.AppendLine($"Tìm thấy {cars.Count} xe. Gợi ý {take} xe phù hợp nhất:");
        sb.AppendLine();

        for (var i = 0; i < take; i++)
        {
            var car = cars[i];
            var statusLabel = car.Status switch
            {
                CarStatusCatalog.Promotion => "Khuyến mãi",
                _ => "Còn hàng"
            };

            sb.Append("- ");
            sb.Append($"#{car.Id} {car.BrandName} {car.Name}");
            sb.Append($" | Link: /cars/{car.Id}");
            sb.Append($" | Loại: {car.Type ?? "-"}");
            sb.Append($" | Năm: {(car.Year?.ToString(CultureInfo.InvariantCulture) ?? "-")}");
            sb.Append($" | Màu: {car.Color ?? "-"}");
            sb.Append($" | Giá: {car.Price.ToString("N0", CultureInfo.InvariantCulture)} VND");
            sb.Append($" | Tồn: {car.StockQuantity}");
            sb.Append($" | {statusLabel}");
            sb.AppendLine();

            var reasons = BuildReasons(search, car);
            if (reasons.Count > 0)
            {
                sb.AppendLine($"  Ly do: {string.Join("; ", reasons)}.");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Nếu bạn muốn so sánh 2 xe, hãy gửi theo mẫu: 'so sánh id 2 và id 5'.");

        return new AiChatResult(sb.ToString().Trim(), Provider: "Database");
    }

    private static AiChatResult BuildDeterministicAdvisorNoMatchAnswer(
        string userMessage,
        CarChatSearchRequest search,
        IReadOnlyList<CarChatCatalogItem> nearestCars)
    {
        var sb = new StringBuilder();
        // Không nhắc lại nội dung người dùng vừa nhập.

        sb.AppendLine("Hiện tại không tìm thấy xe trùng khớp với các tiêu chí vừa nêu.");

        if (search.MaxPrice is not null)
        {
            sb.AppendLine($"- Ngân sách tối đa: {search.MaxPrice.Value.ToString("N0", CultureInfo.InvariantCulture)} VND");
        }

        if (!string.IsNullOrWhiteSpace(search.Type))
        {
            sb.AppendLine($"- Loại xe: {search.Type}");
        }

        if (search.YearFrom is not null || search.YearTo is not null)
        {
            var from = search.YearFrom?.ToString(CultureInfo.InvariantCulture) ?? "-";
            var to = search.YearTo?.ToString(CultureInfo.InvariantCulture) ?? "-";
            sb.AppendLine($"- Năm: {from} den {to}");
        }

        if (nearestCars.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("3 lựa chọn gần nhất (theo giá rẻ nhất trong showroom với các tiêu chí có thể áp dụng):");
            foreach (var car in nearestCars.Take(3))
            {
                sb.Append("- ");
                sb.Append($"#{car.Id} {car.BrandName} {car.Name}");
                sb.Append($" | Link: /cars/{car.Id}");
                sb.Append($" | Loại: {car.Type ?? "-"}");
                sb.Append($" | Năm: {(car.Year?.ToString(CultureInfo.InvariantCulture) ?? "-")}");
                sb.Append($" | Giá: {car.Price.ToString("N0", CultureInfo.InvariantCulture)} VND");
                sb.AppendLine();
            }

            if (search.MaxPrice is not null)
            {
                var cheapest = nearestCars.MinBy(c => c.Price);
                if (cheapest is not null && cheapest.Price > search.MaxPrice.Value)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Gợi ý: Để có thêm lựa chọn, bạn có thể tăng ngân sách tối thiểu lên khoảng {cheapest.Price.ToString("N0", CultureInfo.InvariantCulture)} VND (giá rẻ nhất hiện có).");
                }
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("Hiện tại showroom chưa có xe đang bán/khuyến mãi trong danh mục.");
        }


        sb.AppendLine();
        sb.AppendLine("Bạn cho mình biết thêm (tối đa 2 ý) để lọc đúng hơn:");
        if (search.MaxPrice is null && search.MinPrice is null)
        {
            sb.AppendLine("- Ngân sách tối đa (VND) hoặc khoảng giá?");
        }

        if (string.IsNullOrWhiteSpace(search.Type))
        {
            sb.AppendLine("- Bạn muốn loại xe nào (SUV/Sedan/Hatchback/Pickup)?");
        }
        else
        {
            sb.AppendLine("- Bạn ưu tiên tiêu chí nào nhất: tiết kiệm nhiên liệu, rộng rãi, hay dễ lái?");
        }

        return new AiChatResult(sb.ToString().Trim(), Provider: "Database");
    }

    private static AiChatResult BuildInventoryCountAnswer(
        string userMessage,
        IReadOnlyList<CarListItemViewModel> allCars,
        IReadOnlyList<CarChatCatalogItem> availableCars)
    {
        var totalCars = allCars.Count;
        var totalStock = allCars.Sum(item => item.StockQuantity);
        var availableCount = availableCars.Count;
        var promoCount = availableCars.Count(item => item.Status == CarStatusCatalog.Promotion);

        var sb = new StringBuilder();
        sb.AppendLine("Thống kê từ database:");
        sb.AppendLine();
        sb.AppendLine($"- Tổng số mẫu xe đang quản lý: {totalCars}");
        sb.AppendLine($"- Tổng số xe tồn kho (StockQuantity): {totalStock}");
        sb.AppendLine($"- Số mẫu xe đang bán (Còn hàng/Khuyến mãi): {availableCount}");
        sb.AppendLine($"- Trong đó đang khuyến mãi: {promoCount}");

        if (availableCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Danh sách xe đang bán:");
            foreach (var car in availableCars.Take(Math.Min(10, availableCount)))
            {
                sb.AppendLine($"- #{car.Id} {car.BrandName} {car.Name} | /cars/{car.Id}");
            }

            if (availableCount > 10)
            {
                sb.AppendLine($"- ... ({availableCount - 10} xe khac)");
            }
        }

        return new AiChatResult(sb.ToString().Trim(), Provider: "Database");
    }

    private static IReadOnlyList<string> BuildReasons(CarChatSearchRequest search, CarChatCatalogItem car)
    {
        var reasons = new List<string>();

        if (!string.IsNullOrWhiteSpace(search.Type) &&
            !string.IsNullOrWhiteSpace(car.Type) &&
            string.Equals(search.Type, car.Type, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add($"Dung loai {car.Type}");
        }

        if (search.MaxPrice is not null && car.Price <= search.MaxPrice.Value)
        {
            reasons.Add("Trong ngan sach toi da");
        }
        else if (search.MinPrice is not null && car.Price >= search.MinPrice.Value)
        {
            reasons.Add("Đạt mức giá tối thiểu");
        }

        if (search.YearFrom is not null && car.Year is not null && car.Year.Value >= search.YearFrom.Value)
        {
            reasons.Add("Năm sản xuất phù hợp");
        }
        else if (search.YearTo is not null && car.Year is not null && car.Year.Value <= search.YearTo.Value)
        {
            reasons.Add("Năm sản xuất phù hợp");
        }

        if (car.Status == CarStatusCatalog.Promotion)
        {
            reasons.Add("Đang khuyến mãi");
        }

        if (car.StockQuantity <= 1)
        {
            reasons.Add("Số lượng tồn kho thap (nen xem som)");
        }

        return reasons;
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
            Query = ExtractSearchKeywords(userMessage),
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

    private static string? ExtractSearchKeywords(string userMessage)
    {
        // Reduce natural language questions to useful keywords for SQL LIKE.
        // Examples:
        // - "còn xe toyota camry không" -> "toyota camry"
        // - "hiện có bao nhiêu xe" -> null (handled separately)
        var normalized = NormalizeForHeuristics(userMessage);

        var stopPhrases = new[]
        {
            "hien co", "bao nhieu", "co khong", "con khong", "con xe", "xe nao", "showroom", "trong showroom",
            "toi muon", "ban co", "cho toi", "giup toi", "xin", "tu van", "goi y", "khong", "khong?",
            "ngan sach", "budget", "toi da", "toi thieu", "duoi", "tren", "gia dinh", "tiet kiem", "nhien lieu",
            "di pho", "di du lich", "di lai", "muc dich", "su dung", "so cho", "4 nguoi", "5 cho", "7 cho"
        };

        foreach (var phrase in stopPhrases)
        {
            normalized = normalized.Replace(phrase, " ", StringComparison.Ordinal);
        }

        normalized = normalized
            .Replace("?", " ", StringComparison.Ordinal)
            .Replace(".", " ", StringComparison.Ordinal)
            .Replace(",", " ", StringComparison.Ordinal)
            .Replace("!", " ", StringComparison.Ordinal)
            .Trim();

        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .Where(t => !LooksLikeNumberOrMoneyToken(t))
            .Where(t => !IsGenericQueryToken(t))
            .Distinct()
            .Take(6)
            .ToList();

        if (tokens.Count == 0)
        {
            return null;
        }

        return string.Join(' ', tokens);
    }

    private static bool LooksLikeNumberOrMoneyToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        // Any digits => usually budgets/years/seats; avoid turning it into LIKE query.
        if (token.Any(char.IsDigit))
        {
            return true;
        }

        return token is "vnd" or "đ" or "d" or "dong" or "tr" or "trieu" or "ty" or "m" or "k";
    }

    private static bool IsGenericQueryToken(string token)
    {
        // Keep this list conservative: remove common conversational/criteria words that rarely appear in car names.
        return token is
            "xe" or "mau" or "hang" or "loai" or "nam" or "mau" or "mau?" or "chon" or "nen" or "nao" or
            "tu" or "van" or "goi" or "y" or "loi" or "khuyen" or "thong" or "tin" or "tim" or
            "can" or "muon" or "toi" or "minh" or "ban" or "cho" or "dua" or "tren" or "du" or
            "phu" or "hop" or "nhu" or "cau" or "dieu" or "kien" or "muc" or "dich" or "su" or "dung" or
            "gia" or "dinh" or "tiet" or "kiem" or "nhien" or "lieu" or "nguoi" or "doi" or "tuong" or "lai";
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
        sb.AppendLine("Yêu cầu khách hàng:");
        sb.AppendLine(userMessage);
        sb.AppendLine();
        sb.AppendLine("Bạn là AI tư vấn showroom. TUYỆT ĐỐI chỉ sử dụng thông tin xe trong danh mục dưới đây (dữ liệu từ database) và không được tự chế.");
        sb.AppendLine("Quy tắc bắt buộc:");
        sb.AppendLine("- Nếu đề xuất xe: bắt buộc ghi đúng mã xe theo định dạng '#ID' và kèm link '/cars/ID'.");
        sb.AppendLine("- Giá/năm/loại/màu/tồn kho phải ĐÚNG Ý theo danh mục. Nếu thiếu thông tin thì ghi rõ 'Chưa có dữ liệu'.");
        sb.AppendLine("- Không đề xuất xe không có trong danh mục. Nếu không có xe phù hợp, hãy nói rõ và hỏi tối đa 2 câu để làm rõ (ngân sách, loại xe, số chỗ, mục đích).");
        sb.AppendLine("Trả lời bằng tiếng Việt, gợi ý 2-4 mẫu xe phù hợp, kèm lý do ngắn gọn và tóm tắt giá/năm/loại.");
        sb.AppendLine("Định dạng trả lời: plain text (không dùng markdown, không dùng **, không dùng bảng). Nếu cần liệt kê, dùng đầu dòng '- '.");
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
                CarStatusCatalog.Promotion => "Khuyến mãi",
                _ => "Còn hàng"
            };

            sb.Append("- ");
            sb.Append($"#{car.Id} {car.BrandName} {car.Name}");
            sb.Append($" | {car.Type ?? "-"}");
            sb.Append($" | Năm: {(car.Year?.ToString(CultureInfo.InvariantCulture) ?? "-")}");
            sb.Append($" | Màu: {car.Color ?? "-"}");
            sb.Append($" | Giá: {car.Price.ToString("N0", CultureInfo.InvariantCulture)} VND");
            sb.Append($" | Tồn: {car.StockQuantity}");
            sb.Append($" | {statusLabel}");

            var spec = NormalizeSpec(car.Specifications);
            if (!string.IsNullOrWhiteSpace(spec))
            {
                sb.Append($" | Specs: {spec}");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("Nếu cần so sánh, hãy đề xuất 2 lựa chọn gần nhất (có #ID và link) và so sánh ngắn gọn.");
        return sb.ToString();
    }

    private static string BuildComparePrompt(string userMessage, CarDetailsViewModel left, CarDetailsViewModel right)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Người dùng muốn so sánh 2 xe:");
        sb.AppendLine(userMessage);
        sb.AppendLine();
        sb.AppendLine("Chỉ sử dụng thông tin xe dưới đây (dữ liệu từ database) và không được tự chế. Trả lời bằng tiếng Việt.");
        sb.AppendLine("Bắt buộc so sánh theo: giá, năm, loại, màu, tồn kho, thông số kỹ thuật (nếu có). Kết luận nên chọn xe nào theo từng nhu cầu.");
        sb.AppendLine("Khi nhắc đến xe, hãy ghi #ID và link '/cars/ID'.");
        sb.AppendLine("Định dạng trả lời: plain text (không dùng markdown). Nếu cần liệt kê, dùng đầu dòng '- '.");
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
        sb.AppendLine($"Tên: {car.BrandName} {car.Name}");
        sb.AppendLine($"Loại: {car.Type ?? "-"}");
        sb.AppendLine($"Năm: {(car.Year?.ToString(CultureInfo.InvariantCulture) ?? "-")}");
        sb.AppendLine($"Màu: {car.Color ?? "-"}");
        sb.AppendLine($"Trạng thái: {car.Status}");
        sb.AppendLine($"Giá: {car.Price.ToString("N0", CultureInfo.InvariantCulture)} VND");
        sb.AppendLine($"Tồn kho: {car.StockQuantity}");

        var spec = NormalizeSpec(car.Specifications);
        if (!string.IsNullOrWhiteSpace(spec))
        {
            sb.AppendLine($"Thông số: {spec}");
        }
    }

    private static string? NormalizeSpec(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return null;
        }

        var normalized = NormalizeEscapedNewLines(spec).Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= 220 ? normalized : normalized[..220] + "...";
    }

    private static string NormalizeEscapedNewLines(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\r\\n", Environment.NewLine, StringComparison.Ordinal)
            .Replace("\\n", Environment.NewLine, StringComparison.Ordinal)
            .Replace("\\r", Environment.NewLine, StringComparison.Ordinal);
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
