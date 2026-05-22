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

    public string Sort { get; init; } = PublicCarSortCatalog.Relevance;
}

public static class PublicCarSortCatalog
{
    public const string Relevance = "relevance";
    public const string Newest = "newest";
    public const string PriceAsc = "price-asc";
    public const string PriceDesc = "price-desc";
    public const string YearDesc = "year-desc";

    public static string Normalize(string? sort)
        => sort?.Trim().ToLowerInvariant() switch
        {
            Newest => Newest,
            PriceAsc => PriceAsc,
            PriceDesc => PriceDesc,
            YearDesc => YearDesc,
            _ => Relevance
        };
}
