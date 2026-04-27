namespace Showroom.Web.Models;

public sealed class SalesReportViewModel
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public int? BrandId { get; init; }

    public string BrandLabel { get; init; } = "Tat ca hang";

    public int TotalQuantity { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<SalesReportCarRow> TopCars { get; init; } = Array.Empty<SalesReportCarRow>();

    public IReadOnlyList<SalesReportBrandRow> Brands { get; init; } = Array.Empty<SalesReportBrandRow>();
}

public sealed class SalesReportCarRow
{
    public int CarId { get; init; }

    public string CarName { get; init; } = string.Empty;

    public string BrandName { get; init; } = string.Empty;

    public int SoldQuantity { get; init; }

    public decimal Amount { get; init; }
}

public sealed class SalesReportBrandRow
{
    public string BrandName { get; init; } = string.Empty;

    public int SoldQuantity { get; init; }

    public decimal Amount { get; init; }
}

