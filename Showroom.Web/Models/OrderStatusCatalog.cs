using Microsoft.AspNetCore.Mvc.Rendering;

namespace Showroom.Web.Models;

public static class OrderStatusCatalog
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Completed = "Completed";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    private static readonly HashSet<string> SalesStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        Paid,
        Completed,
        Delivered
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        Paid,
        Completed,
        Delivered,
        Cancelled
    };

    public static IReadOnlyList<SelectListItem> GetSelectList()
        => new[]
        {
            new SelectListItem { Value = Pending, Text = "Chờ xử lý" },
            new SelectListItem { Value = Paid, Text = "Đã thanh toán" },
            new SelectListItem { Value = Completed, Text = "Hoàn tất" },
            new SelectListItem { Value = Delivered, Text = "Đã giao xe" },
            new SelectListItem { Value = Cancelled, Text = "Đã hủy" }
        };

    public static bool CountsTowardSales(string status)
        => SalesStatuses.Contains(status ?? string.Empty);

    public static bool IsValid(string status)
        => ValidStatuses.Contains(status ?? string.Empty);
}
