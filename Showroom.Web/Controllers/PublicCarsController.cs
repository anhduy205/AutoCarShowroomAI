using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Models;
using Showroom.Web.Services;
using Showroom.Web.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace Showroom.Web.Controllers;

[EnableRateLimiting("PublicBrowse")]
public sealed class PublicCarsController : Controller
{
    private static readonly string[] KnownTypes = { "SUV", "Sedan", "Hatchback", "Crossover", "Pickup", "MPV" };

    private readonly IInventoryManagementService _inventoryManagementService;

    public PublicCarsController(IInventoryManagementService inventoryManagementService)
    {
        _inventoryManagementService = inventoryManagementService;
    }

    [HttpGet("/cars")]
    public async Task<IActionResult> Index(
        [FromQuery] string? q,
        [FromQuery] int? brandId,
        [FromQuery] string? type,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? yearFrom,
        [FromQuery] int? yearTo,
        CancellationToken cancellationToken)
    {
        q = InputSanitizer.SanitizeQuery(q, 200);
        type = InputSanitizer.SanitizeQuery(type, 50);

        if (minPrice is < 0)
        {
            minPrice = null;
        }

        if (maxPrice is < 0)
        {
            maxPrice = null;
        }

        if (minPrice is not null && maxPrice is not null && minPrice > maxPrice)
        {
            (minPrice, maxPrice) = (maxPrice, minPrice);
        }

        yearFrom = InputSanitizer.ClampYear(yearFrom);
        yearTo = InputSanitizer.ClampYear(yearTo);

        if (yearFrom is not null && yearTo is not null && yearFrom > yearTo)
        {
            (yearFrom, yearTo) = (yearTo, yearFrom);
        }

        var request = new PublicCarSearchRequest
        {
            Query = q,
            BrandId = brandId,
            Type = type,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            YearFrom = yearFrom,
            YearTo = yearTo
        };

        var cars = await _inventoryManagementService.GetPublicCarsAsync(request, cancellationToken);
        var brands = await _inventoryManagementService.GetBrandOptionsAsync(cancellationToken);

        var model = new PublicCarsIndexViewModel
        {
            Query = q,
            BrandId = brandId,
            Type = type,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            YearFrom = yearFrom,
            YearTo = yearTo,
            Cars = cars,
            BrandOptions = brands,
            TypeOptions = BuildTypeOptions(type)
        };

        return View(model);
    }

    [HttpGet("/cars/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var model = await _inventoryManagementService.GetCarDetailsAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    private static IReadOnlyList<SelectListItem> BuildTypeOptions(string? selected)
    {
        var items = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "Tất cả loại xe" }
        };

        foreach (var knownType in KnownTypes)
        {
            items.Add(new SelectListItem
            {
                Value = knownType,
                Text = knownType,
                Selected = string.Equals(knownType, selected, StringComparison.OrdinalIgnoreCase)
            });
        }

        return items;
    }
}
