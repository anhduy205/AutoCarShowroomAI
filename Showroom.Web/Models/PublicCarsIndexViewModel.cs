using Microsoft.AspNetCore.Mvc.Rendering;

namespace Showroom.Web.Models;

public sealed class PublicCarsIndexViewModel
{
    public string? Query { get; set; }

    public int? BrandId { get; set; }

    public string? Type { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? YearFrom { get; set; }

    public int? YearTo { get; set; }

    public IReadOnlyList<SelectListItem> BrandOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> TypeOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<PublicCarListItemViewModel> Cars { get; set; } = Array.Empty<PublicCarListItemViewModel>();
}

public sealed class PublicCarListItemViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string BrandName { get; init; } = string.Empty;

    public int? Year { get; init; }

    public string? Type { get; init; }

    public string? Color { get; init; }

    public decimal Price { get; init; }

    public string? ThumbnailUrl { get; init; }

    public string Status { get; init; } = CarStatusCatalog.InStock;
}
