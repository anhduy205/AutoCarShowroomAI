namespace Showroom.Web.Models;

public class AdminDashboardViewModel
{
    public int TotalCars { get; init; }

    public IReadOnlyList<BrandInventoryItem> BrandInventory { get; init; } = Array.Empty<BrandInventoryItem>();

    public IReadOnlyList<TopSellingCarItem> BestSellingCars { get; init; } = Array.Empty<TopSellingCarItem>();

    public bool IsDatabaseConnected { get; init; }

    public string StatusMessage { get; init; } = string.Empty;
}
