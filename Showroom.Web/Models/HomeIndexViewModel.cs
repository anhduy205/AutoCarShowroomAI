namespace Showroom.Web.Models;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<PublicCarListItemViewModel> FeaturedCars { get; set; } = Array.Empty<PublicCarListItemViewModel>();

    public string? StatusMessage { get; set; }
}

