using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface IReportService
{
    Task<IReadOnlyList<SelectListItem>> GetBrandOptionsAsync(CancellationToken cancellationToken = default);

    Task<SalesReportViewModel> GetSalesReportAsync(SalesReportRequest request, CancellationToken cancellationToken = default);

    Task<InventoryReportViewModel> GetInventoryReportAsync(InventoryReportRequest request, CancellationToken cancellationToken = default);
}

public sealed class SalesReportRequest
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public int? BrandId { get; init; }

    public int Take { get; init; } = 50;
}

public sealed class InventoryReportRequest
{
    public int? BrandId { get; init; }

    public string? Status { get; init; }

    public int Take { get; init; } = 200;
}

