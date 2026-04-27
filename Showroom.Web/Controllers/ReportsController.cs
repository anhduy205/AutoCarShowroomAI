using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.ReportViewer)]
public sealed class ReportsController : Controller
{
    private static readonly IReadOnlyList<SelectListItem> InventoryStatusOptions = new[]
    {
        new SelectListItem { Value = "", Text = "Tat ca trang thai" },
        new SelectListItem { Value = CarStatusCatalog.InStock, Text = "InStock" },
        new SelectListItem { Value = CarStatusCatalog.Promotion, Text = "Promotion" },
        new SelectListItem { Value = CarStatusCatalog.Sold, Text = "Sold" }
    };

    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("/Reports")]
    public async Task<IActionResult> Index(
        [FromQuery] string? tab,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? brandId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        tab = string.IsNullOrWhiteSpace(tab) ? "sales" : tab.Trim().ToLowerInvariant();
        tab = tab is "inventory" ? "inventory" : "sales";

        var brandOptions = await _reports.GetBrandOptionsAsync(cancellationToken);
        var model = new ReportsIndexViewModel
        {
            Tab = tab,
            BrandOptions = BuildBrandOptions(brandOptions, brandId),
            InventoryStatusOptions = BuildStatusOptions(status),
            From = from,
            To = to,
            BrandId = brandId,
            Status = status
        };

        if (tab == "inventory")
        {
            model.Inventory = await _reports.GetInventoryReportAsync(
                new InventoryReportRequest
                {
                    BrandId = brandId,
                    Status = status,
                    Take = 300
                },
                cancellationToken);

            return View(model);
        }

        model.Sales = await _reports.GetSalesReportAsync(
            new SalesReportRequest
            {
                From = from,
                To = to,
                BrandId = brandId,
                Take = 50
            },
            cancellationToken);

        return View(model);
    }

    [HttpGet("/Reports/Sales.csv")]
    public async Task<IActionResult> SalesCsv(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? brandId,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetSalesReportAsync(
            new SalesReportRequest { From = from, To = to, BrandId = brandId, Take = 500 },
            cancellationToken);

        var csv = BuildSalesCsv(report);
        var bytes = SqlReportService.ToCsvWithBom(csv);
        var filename = $"sales-report-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }

    [HttpGet("/Reports/Inventory.csv")]
    public async Task<IActionResult> InventoryCsv(
        [FromQuery] int? brandId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetInventoryReportAsync(
            new InventoryReportRequest { BrandId = brandId, Status = status, Take = 2000 },
            cancellationToken);

        var csv = BuildInventoryCsv(report);
        var bytes = SqlReportService.ToCsvWithBom(csv);
        var filename = $"inventory-report-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }

    private static IReadOnlyList<SelectListItem> BuildBrandOptions(IReadOnlyList<SelectListItem> items, int? selectedId)
    {
        var options = new List<SelectListItem> { new() { Value = "", Text = "Tat ca hang" } };
        foreach (var item in items)
        {
            options.Add(new SelectListItem
            {
                Value = item.Value,
                Text = item.Text,
                Selected = selectedId is not null && item.Value == selectedId.Value.ToString(CultureInfo.InvariantCulture)
            });
        }

        return options;
    }

    private static IReadOnlyList<SelectListItem> BuildStatusOptions(string? selected)
        => InventoryStatusOptions
            .Select(option => new SelectListItem
            {
                Value = option.Value,
                Text = option.Text,
                Selected = string.Equals(option.Value, selected ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

    private static string BuildSalesCsv(SalesReportViewModel report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CarId,CarName,Brand,SoldQuantity,Amount");
        foreach (var row in report.TopCars)
        {
            sb.Append(SqlReportService.CsvEscape(row.CarId.ToString(CultureInfo.InvariantCulture)));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.CarName));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.BrandName));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.SoldQuantity.ToString(CultureInfo.InvariantCulture)));
            sb.Append(',');
            sb.AppendLine(SqlReportService.CsvEscape(row.Amount.ToString(CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildInventoryCsv(InventoryReportViewModel report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CarId,CarName,Brand,Status,Price,StockQuantity,Year,Type");
        foreach (var row in report.Cars)
        {
            sb.Append(SqlReportService.CsvEscape(row.CarId.ToString(CultureInfo.InvariantCulture)));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.CarName));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.BrandName));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.Status));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.Price.ToString(CultureInfo.InvariantCulture)));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.StockQuantity.ToString(CultureInfo.InvariantCulture)));
            sb.Append(',');
            sb.Append(SqlReportService.CsvEscape(row.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty));
            sb.Append(',');
            sb.AppendLine(SqlReportService.CsvEscape(row.Type ?? string.Empty));
        }

        return sb.ToString();
    }
}

public sealed class ReportsIndexViewModel
{
    public string Tab { get; init; } = "sales";

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public int? BrandId { get; init; }

    public string? Status { get; init; }

    public IReadOnlyList<SelectListItem> BrandOptions { get; init; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> InventoryStatusOptions { get; init; } = Array.Empty<SelectListItem>();

    public SalesReportViewModel? Sales { get; set; }

    public InventoryReportViewModel? Inventory { get; set; }
}

