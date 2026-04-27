namespace Showroom.Web.Models;

public sealed class CarChatCatalogItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string BrandName { get; init; } = string.Empty;

    public int? Year { get; init; }

    public string? Type { get; init; }

    public string? Color { get; init; }

    public string Status { get; init; } = CarStatusCatalog.InStock;

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }

    public string? Specifications { get; init; }
}

