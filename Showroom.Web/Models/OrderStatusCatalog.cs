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

    public static IReadOnlyList<SelectListItem> GetSelectList()
        => new[]
        {
            new SelectListItem { Value = Pending, Text = "Cho xu ly" },
            new SelectListItem { Value = Paid, Text = "Da thanh toan" },
            new SelectListItem { Value = Completed, Text = "Hoan tat" },
            new SelectListItem { Value = Delivered, Text = "Da giao xe" },
            new SelectListItem { Value = Cancelled, Text = "Da huy" }
        };

    public static bool CountsTowardSales(string status)
        => SalesStatuses.Contains(status ?? string.Empty);
}
