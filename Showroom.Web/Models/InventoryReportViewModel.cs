namespace Showroom.Web.Models;

public sealed class InventoryReportViewModel
{
    public int? BrandId { get; init; }

    public string BrandLabel { get; init; } = "Tat ca hang";

    public string? Status { get; init; }

    public int TotalCars { get; init; }

    public int TotalStockQuantity { get; init; }

    public IReadOnlyList<InventoryReportRow> Cars { get; init; } = Array.Empty<InventoryReportRow>();

    public IReadOnlyList<InventoryStatusSummaryRow> StatusSummary { get; init; } = Array.Empty<InventoryStatusSummaryRow>();
}

public sealed class InventoryReportRow
{
    public int CarId { get; init; }

    public string CarName { get; init; } = string.Empty;

    public string BrandName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }

    public int? Year { get; init; }

    public string? Type { get; init; }
}

public sealed class InventoryStatusSummaryRow
{
    public string Status { get; init; } = string.Empty;

    public int CarCount { get; init; }

    public int StockQuantity { get; init; }
}

