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
            throw new FriendlyOperationException("Noi dung tin nhan khong duoc de trong.");
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
        var cars = RankCarsForAdvice(await _inventory.GetCarsForChatAsync(search, cancellationToken), search);

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
                nearest = RankCarsForAdvice(await _inventory.GetCarsForChatAsync(new CarChatSearchRequest { Take = 8 }, cancellationToken), search)
                    .Take(3)
                    .ToList();
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
        sb.AppendLine($"Thong tin xe #{car.Id} ({car.BrandName} {car.Name})");
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
        // Khong nhac lai noi dung nguoi dung vua nhap.

        if (cars.Count == 0)
        {
            sb.AppendLine("Hien tai khong co xe phu hop trong danh muc.");
            sb.AppendLine("Ban cho minh biet them (toi da 2 y):");
            sb.AppendLine("- Ngan sach toi da (VND) hoac khoang gia?");
            sb.AppendLine("- Can loai xe/so cho/muc dich su dung (di pho/di du lich/gia dinh)?");
            return new AiChatResult(sb.ToString().Trim(), Provider: "Database");
        }

        var take = Math.Min(4, cars.Count);
        sb.AppendLine("Tu van chon xe theo nhu cau:");
        AppendDetectedNeeds(sb, search);
        sb.AppendLine();
        sb.AppendLine($"Tim thay {cars.Count} xe trong database. Goi y {take} xe phu hop nhat:");
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
            sb.Append($" | Ton: {car.StockQuantity}");
            sb.Append($" | {statusLabel}");
            sb.AppendLine();

            var reasons = BuildReasons(search, car);
            if (reasons.Count > 0)
            {
                sb.AppendLine($"  Ly do: {string.Join("; ", reasons)}.");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Neu ban muon so sanh 2 xe, hay gui theo mau: 'so sanh id 2 va id 5'.");

        return new AiChatResult(sb.ToString().Trim(), Provider: "Database");
    }

    private static AiChatResult BuildDeterministicAdvisorNoMatchAnswer(
        string userMessage,
        CarChatSearchRequest search,
        IReadOnlyList<CarChatCatalogItem> nearestCars)
    {
        var sb = new StringBuilder();
        // Khong nhac lai noi dung nguoi dung vua nhap.

        sb.AppendLine("Hien tai khong tim thay xe trung khop voi cac tieu chi vua neu.");

        if (search.MaxPrice is not null)
        {
            sb.AppendLine($"- Ngan sach toi da: {search.MaxPrice.Value.ToString("N0", CultureInfo.InvariantCulture)} VND");
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
            sb.AppendLine("3 lua chon gan nhat (theo gia re nhat trong showroom voi cac tieu chi co the ap dung):");
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
                    sb.AppendLine($"Goi y: De co them lua chon, ban co the tang ngan sach toi thieu len khoang {cheapest.Price.ToString("N0", CultureInfo.InvariantCulture)} VND (gia re nhat hien co).");
                }
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("Hien tai showroom chua co xe dang ban/khuyen mai trong danh muc.");
        }


        sb.AppendLine();
        sb.AppendLine("Ban cho minh biet them (toi da 2 y) de loc dung hon:");
        if (search.MaxPrice is null && search.MinPrice is null)
        {
            sb.AppendLine("- Ngan sach toi da (VND) hoac khoang gia?");
        }

        if (string.IsNullOrWhiteSpace(search.Type))
        {
            sb.AppendLine("- Ban muon loai xe nao (SUV/Sedan/Hatchback/Pickup)?");
        }
        else
        {
            sb.AppendLine("- Ban uu tien tieu chi nao nhat: tiet kiem nhien lieu, rong rai, hay de lai?");
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
        sb.AppendLine("Thong ke tu database:");
        sb.AppendLine();
        sb.AppendLine($"- Tong so mau xe dang quan ly: {totalCars}");
        sb.AppendLine($"- Tong so xe ton kho (StockQuantity): {totalStock}");
        sb.AppendLine($"- So mau xe dang ban (Còn hàng/Khuyến mãi): {availableCount}");
        sb.AppendLine($"- Trong do dang khuyen mai: {promoCount}");

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
            reasons.Add("Dat muc gia toi thieu");
        }

        if (search.YearFrom is not null && car.Year is not null && car.Year.Value >= search.YearFrom.Value)
        {
            reasons.Add("Năm san xuat phu hop");
        }
        else if (search.YearTo is not null && car.Year is not null && car.Year.Value <= search.YearTo.Value)
        {
            reasons.Add("Năm san xuat phu hop");
        }

        if (car.Status == CarStatusCatalog.Promotion)
        {
            reasons.Add("Dang khuyen mai");
        }

        if (search.Seats is not null && LooksSuitableForSeats(car, search.Seats.Value))
        {
            reasons.Add($"Phu hop nhu cau khoang {search.Seats.Value} cho");
        }

        if (string.Equals(search.Purpose, "Family", StringComparison.OrdinalIgnoreCase) &&
            (IsRoomyFamilyCar(car) || LooksSuitableForSeats(car, 5)))
        {
            reasons.Add("Hop nhu cau gia dinh");
        }

        if (string.Equals(search.Purpose, "City", StringComparison.OrdinalIgnoreCase) &&
            IsEasyCityCar(car))
        {
            reasons.Add("De di pho va xoay xo trong do thi");
        }

        if (string.Equals(search.Purpose, "Service", StringComparison.OrdinalIgnoreCase) &&
            IsServiceFriendlyCar(car))
        {
            reasons.Add("Hop chay dich vu/di lai nhieu");
        }

        if (string.Equals(search.Purpose, "Business", StringComparison.OrdinalIgnoreCase) &&
            IsBusinessCar(car))
        {
            reasons.Add("Hop nhu cau cong tac/doanh nhan");
        }

        if (string.Equals(search.Priority, "Saving", StringComparison.OrdinalIgnoreCase) &&
            LooksEfficient(car))
        {
            reasons.Add("Uu tien tiet kiem nhien lieu/chi phi");
        }

        if (string.Equals(search.Priority, "Safety", StringComparison.OrdinalIgnoreCase) &&
            LooksSafe(car))
        {
            reasons.Add("Co diem phu hop ve an toan/cong nghe ho tro");
        }

        if (string.Equals(search.Priority, "Comfort", StringComparison.OrdinalIgnoreCase) &&
            LooksComfortable(car))
        {
            reasons.Add("Uu tien em ai/rong rai/tien nghi");
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
        else if (normalizedMessage.Contains("crossover", StringComparison.Ordinal))
        {
            request = request with { Type = "Crossover" };
        }
        else if (normalizedMessage.Contains("mpv", StringComparison.Ordinal))
        {
            request = request with { Type = "MPV" };
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

        var seats = TryExtractSeatCount(normalizedMessage);
        if (seats is not null)
        {
            request = request with { Seats = seats };
        }

        var purpose = InferPurpose(normalizedMessage);
        if (!string.IsNullOrWhiteSpace(purpose))
        {
            request = request with { Purpose = purpose };
        }

        var priority = InferPriority(normalizedMessage);
        if (!string.IsNullOrWhiteSpace(priority))
        {
            request = request with { Priority = priority };
        }

        return request;
    }

    private static IReadOnlyList<CarChatCatalogItem> RankCarsForAdvice(
        IReadOnlyList<CarChatCatalogItem> cars,
        CarChatSearchRequest search)
        => cars
            .Select(car => new { Car = car, Score = CalculateAdviceScore(car, search) })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Car.Status == CarStatusCatalog.Promotion ? 0 : 1)
            .ThenBy(item => item.Car.Price)
            .Select(item => item.Car)
            .ToList();

    private static int CalculateAdviceScore(CarChatCatalogItem car, CarChatSearchRequest search)
    {
        var score = 0;

        if (car.Status == CarStatusCatalog.Promotion)
        {
            score += 4;
        }

        if (search.MaxPrice is not null && car.Price <= search.MaxPrice.Value)
        {
            score += 8;
        }

        if (search.MinPrice is not null && car.Price >= search.MinPrice.Value)
        {
            score += 3;
        }

        if (search.Seats is not null && LooksSuitableForSeats(car, search.Seats.Value))
        {
            score += 12;
        }

        score += search.Purpose switch
        {
            "Family" when IsRoomyFamilyCar(car) => 12,
            "City" when IsEasyCityCar(car) => 12,
            "Service" when IsServiceFriendlyCar(car) => 12,
            "Travel" when IsTravelFriendlyCar(car) => 10,
            "Business" when IsBusinessCar(car) => 10,
            _ => 0
        };

        score += search.Priority switch
        {
            "Saving" when LooksEfficient(car) => 10,
            "Safety" when LooksSafe(car) => 10,
            "Comfort" when LooksComfortable(car) => 10,
            "Power" when LooksPowerful(car) => 8,
            _ => 0
        };

        if (car.StockQuantity <= 1)
        {
            score -= 1;
        }

        return score;
    }

    private static void AppendDetectedNeeds(StringBuilder sb, CarChatSearchRequest search)
    {
        var needs = new List<string>();
        if (search.MaxPrice is not null)
        {
            needs.Add($"ngan sach toi da {search.MaxPrice.Value.ToString("N0", CultureInfo.InvariantCulture)} VND");
        }

        if (search.Seats is not null)
        {
            needs.Add($"khoang {search.Seats.Value} cho");
        }

        if (!string.IsNullOrWhiteSpace(search.Type))
        {
            needs.Add($"loai {search.Type}");
        }

        var purpose = search.Purpose switch
        {
            "Family" => "muc dich gia dinh",
            "City" => "di pho",
            "Service" => "chay dich vu/di lai nhieu",
            "Travel" => "di xa/du lich",
            "Business" => "cong tac/doanh nhan",
            _ => null
        };

        if (purpose is not null)
        {
            needs.Add(purpose);
        }

        var priority = search.Priority switch
        {
            "Saving" => "uu tien tiet kiem",
            "Safety" => "uu tien an toan",
            "Comfort" => "uu tien rong rai/em ai",
            "Power" => "uu tien van hanh manh",
            _ => null
        };

        if (priority is not null)
        {
            needs.Add(priority);
        }

        if (needs.Count > 0)
        {
            sb.AppendLine($"Nhu cau da nhan dien: {string.Join("; ", needs)}.");
        }
    }

    private static int? TryExtractSeatCount(string normalizedMessage)
    {
        var match = Regex.Match(normalizedMessage, @"\b(?<seats>[2456789])\s*(cho|nguoi)\b", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups["seats"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seats)
            ? seats
            : null;
    }

    private static string? InferPurpose(string normalizedMessage)
    {
        if (ContainsAny(normalizedMessage, "gia dinh", "tre em", "vo con", "ca nha"))
        {
            return "Family";
        }

        if (ContainsAny(normalizedMessage, "di pho", "do thi", "di lam hang ngay", "moi lai", "de lai"))
        {
            return "City";
        }

        if (ContainsAny(normalizedMessage, "chay dich vu", "grab", "taxi", "kinh doanh", "di lai nhieu"))
        {
            return "Service";
        }

        if (ContainsAny(normalizedMessage, "du lich", "di xa", "duong truong", "cop lon", "hanh ly"))
        {
            return "Travel";
        }

        if (ContainsAny(normalizedMessage, "doanh nhan", "cong tac", "xe sang", "doi tac"))
        {
            return "Business";
        }

        return null;
    }

    private static string? InferPriority(string normalizedMessage)
    {
        if (ContainsAny(normalizedMessage, "tiet kiem", "it hao xang", "hao xang", "nhien lieu", "chi phi thap"))
        {
            return "Saving";
        }

        if (ContainsAny(normalizedMessage, "an toan", "canh bao", "abs", "tui khi", "diem mu"))
        {
            return "Safety";
        }

        if (ContainsAny(normalizedMessage, "rong rai", "em ai", "cach am", "tien nghi", "thoai mai", "cop lon"))
        {
            return "Comfort";
        }

        if (ContainsAny(normalizedMessage, "manh", "tang toc", "dong co", "cam giac lai", "the thao"))
        {
            return "Power";
        }

        return null;
    }

    private static bool LooksSuitableForSeats(CarChatCatalogItem car, int seats)
    {
        var text = NormalizeCarText(car);
        if (ContainsAny(text, $"{seats} cho", $"{seats}-cho", $"{seats}cho", $"{seats} seats"))
        {
            return true;
        }

        if (seats >= 7)
        {
            return ContainsAny(text, "7 cho", "mpv", "suv", "crossover", "minivan", "stargazer", "santa fe", "territory", "everest", "fortuner");
        }

        if (seats <= 5)
        {
            return ContainsAny(text, "5 cho", "sedan", "hatchback", "crossover", "suv");
        }

        return false;
    }

    private static bool IsRoomyFamilyCar(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "suv", "crossover", "mpv", "7 cho", "rong rai", "cop", "santa fe", "creta", "territory", "stargazer", "venue");

    private static bool IsEasyCityCar(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "sedan", "hatchback", "compact", "city", "accent", "venue", "mazda 2", "creta");

    private static bool IsServiceFriendlyCar(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "sedan", "mpv", "hybrid", "tiet kiem", "accent", "vios", "stargazer", "innova", "venue");

    private static bool IsTravelFriendlyCar(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "suv", "crossover", "mpv", "ban tai", "pickup", "7 cho", "cop", "everest", "santa fe", "territory", "ranger");

    private static bool IsBusinessCar(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "sedan", "suv", "luxury", "premium", "camry", "mazda 6", "santa fe", "territory");

    private static bool LooksEfficient(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "hybrid", "tiet kiem", "eco", "sedan", "hatchback", "1.5", "1.4", "venue", "accent", "mazda 2");

    private static bool LooksSafe(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "abs", "esc", "canh bao", "diem mu", "tui khi", "camera", "phanh", "safety", "an toan", "adas");

    private static bool LooksComfortable(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "rong rai", "em ai", "cach am", "da", "premium", "suv", "mpv", "7 cho", "santa fe", "territory");

    private static bool LooksPowerful(CarChatCatalogItem car)
        => ContainsAny(NormalizeCarText(car), "turbo", "2.0", "2.5", "diesel", "ban tai", "pickup", "ranger", "everest", "santa fe");

    private static string NormalizeCarText(CarChatCatalogItem car)
        => NormalizeForHeuristics($"{car.BrandName} {car.Name} {car.Type} {car.Color} {car.Specifications}");

    private static bool ContainsAny(string value, params string[] needles)
        => needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static string? ExtractSearchKeywords(string userMessage)
    {
        // Reduce natural language questions to useful keywords for SQL LIKE.
        // Examples:
        // - "con xe toyota camry khong" -> "toyota camry"
        // - "hien co bao nhieu xe" -> null (handled separately)
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
        sb.AppendLine("Yeu cau khach hang:");
        sb.AppendLine(userMessage);
        sb.AppendLine();
        sb.AppendLine("Ban la AI tu van showroom. TUYET DOI chi su dung thong tin xe trong danh muc duoi day (du lieu tu database) va khong duoc tu che.");
        sb.AppendLine("Quy tac bat buoc:");
        sb.AppendLine("- Neu de xuat xe: bat buoc ghi dung ma xe theo dinh dang '#ID' va kem link '/cars/ID'.");
        sb.AppendLine("- Giá/nam/loai/mau/ton kho phai DUNG Y theo danh muc. Neu thieu thong tin thi ghi ro 'Chua co du lieu'.");
        sb.AppendLine("- Khong de xuat xe khong co trong danh muc. Neu khong co xe phu hop, hay noi ro va hoi toi da 2 cau de lam ro (ngan sach, loai xe, so cho, muc dich).");
        sb.AppendLine("Tra loi bang tieng Viet, goi y 2-4 mau xe phu hop, kem ly do ngan gon va tom tat gia/nam/loai.");
        sb.AppendLine("Dinh dang tra loi: plain text (khong dung markdown, khong dung **, khong dung bang). Neu can liet ke, dung dau dong '- '.");
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
        sb.AppendLine("Neu can so sanh, hay de xuat 2 lua chon gan nhat (co #ID va link) va so sanh ngan gon.");
        return sb.ToString();
    }

    private static string BuildComparePrompt(string userMessage, CarDetailsViewModel left, CarDetailsViewModel right)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Nguoi dung muon so sanh 2 xe:");
        sb.AppendLine(userMessage);
        sb.AppendLine();
        sb.AppendLine("Chi su dung thong tin xe duoi day (du lieu tu database) va khong duoc tu che. Tra loi bang tieng Viet.");
        sb.AppendLine("Bat buoc so sanh theo: gia, nam, loai, mau, ton kho, thong so ky thuat (neu co). Ket luan nen chon xe nao theo tung nhu cau.");
        sb.AppendLine("Khi nhac den xe, hay ghi #ID va link '/cars/ID'.");
        sb.AppendLine("Dinh dang tra loi: plain text (khong dung markdown). Neu can liet ke, dung dau dong '- '.");
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
        sb.AppendLine($"Loại: {car.Type ?? "-"}");
        sb.AppendLine($"Năm: {(car.Year?.ToString(CultureInfo.InvariantCulture) ?? "-")}");
        sb.AppendLine($"Màu: {car.Color ?? "-"}");
        sb.AppendLine($"Trạng thái: {car.Status}");
        sb.AppendLine($"Giá: {car.Price.ToString("N0", CultureInfo.InvariantCulture)} VND");
        sb.AppendLine($"Tồn kho: {car.StockQuantity}");

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
