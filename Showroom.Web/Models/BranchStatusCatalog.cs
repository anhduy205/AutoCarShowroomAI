using Microsoft.AspNetCore.Mvc.Rendering;

namespace Showroom.Web.Models;

public static class BranchStatusCatalog
{
    public const string Active = "Active";

    public const string Inactive = "Inactive";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null)
    {
        var normalized = string.IsNullOrWhiteSpace(selected) ? Active : selected.Trim();

        return new[]
        {
            new SelectListItem { Value = Active, Text = "Đang hoạt động", Selected = normalized == Active },
            new SelectListItem { Value = Inactive, Text = "Tạm ngưng", Selected = normalized == Inactive }
        };
    }

    public static string Normalize(string? status)
        => string.Equals(status, Inactive, StringComparison.OrdinalIgnoreCase) ? Inactive : Active;

    public static string GetDisplayName(string? status)
        => string.Equals(status, Inactive, StringComparison.OrdinalIgnoreCase) ? "Tạm ngưng" : "Đang hoạt động";
}
