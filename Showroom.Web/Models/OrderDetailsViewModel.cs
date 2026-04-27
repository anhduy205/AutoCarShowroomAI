namespace Showroom.Web.Models;

public sealed class OrderDetailsViewModel
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerAddress { get; set; }

    public string? Note { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<OrderDetailsItemViewModel> Items { get; set; } = Array.Empty<OrderDetailsItemViewModel>();

    public int TotalQuantity => Items.Sum(item => item.Quantity);

    public decimal TotalAmount => Items.Sum(item => item.LineTotal);
}

public sealed class OrderDetailsItemViewModel
{
    public int CarId { get; set; }

    public string CarName { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
