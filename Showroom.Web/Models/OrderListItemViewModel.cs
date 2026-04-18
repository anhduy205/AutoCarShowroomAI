namespace Showroom.Web.Models;

public class OrderListItemViewModel
{
    public int Id { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int TotalQuantity { get; init; }

    public decimal TotalAmount { get; init; }

    public DateTime CreatedAt { get; init; }
}
