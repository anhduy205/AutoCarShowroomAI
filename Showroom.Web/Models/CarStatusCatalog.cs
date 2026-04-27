using Microsoft.AspNetCore.Mvc.Rendering;

namespace Showroom.Web.Models;

public static class CarStatusCatalog
{
    public const string InStock = "InStock";
    public const string Sold = "Sold";
    public const string Promotion = "Promotion";

    private static readonly IReadOnlyList<string> Statuses = new[] { InStock, Promotion, Sold };

    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && Statuses.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null)
        => new[]
        {
            new SelectListItem { Value = InStock, Text = "Còn hàng", Selected = IsSelected(InStock, selected) },
            new SelectListItem { Value = Promotion, Text = "Khuyến mãi", Selected = IsSelected(Promotion, selected) },
            new SelectListItem { Value = Sold, Text = "Đã bán", Selected = IsSelected(Sold, selected) }
        };

    private static bool IsSelected(string value, string? selected)
        => !string.IsNullOrWhiteSpace(selected) && string.Equals(value, selected.Trim(), StringComparison.OrdinalIgnoreCase);
}

