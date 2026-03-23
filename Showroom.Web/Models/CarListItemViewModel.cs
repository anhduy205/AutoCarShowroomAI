namespace Showroom.Web.Models;

public class CarListItemViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string BrandName { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }

    public DateTime CreatedAt { get; init; }
}
