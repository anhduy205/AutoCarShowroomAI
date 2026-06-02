using Microsoft.AspNetCore.Mvc.Rendering;

namespace Showroom.Web.Models;

public static class CustomerTypeCatalog
{
    public const string Individual = "Individual";

    public const string Company = "Company";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null)
    {
        var normalized = string.IsNullOrWhiteSpace(selected) ? Individual : selected.Trim();

        return new[]
        {
            new SelectListItem { Value = Individual, Text = "Cá nhân", Selected = normalized == Individual },
            new SelectListItem { Value = Company, Text = "Doanh nghiệp", Selected = normalized == Company }
        };
    }

    public static string Normalize(string? customerType)
        => string.Equals(customerType, Company, StringComparison.OrdinalIgnoreCase) ? Company : Individual;

    public static string GetDisplayName(string? customerType)
        => string.Equals(customerType, Company, StringComparison.OrdinalIgnoreCase) ? "Doanh nghiệp" : "Cá nhân";
}
