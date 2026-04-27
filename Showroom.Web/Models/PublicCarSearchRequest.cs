namespace Showroom.Web.Models;

public sealed class PublicCarSearchRequest
{
    public string? Query { get; init; }

    public int? BrandId { get; init; }

    public string? Type { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public int? YearFrom { get; init; }

    public int? YearTo { get; init; }
}

