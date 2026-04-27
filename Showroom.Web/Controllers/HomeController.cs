using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Showroom.Web.Models;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[EnableRateLimiting("PublicBrowse")]
public class HomeController : Controller
{
    private readonly IInventoryManagementService _inventory;

    public HomeController(IInventoryManagementService inventory)
    {
        _inventory = inventory;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new HomeIndexViewModel();

        try
        {
            var cars = await _inventory.GetPublicCarsAsync(new PublicCarSearchRequest(), cancellationToken);
            model.FeaturedCars = cars.Take(6).ToList();
        }
        catch (FriendlyOperationException ex)
        {
            model.StatusMessage = ex.Message;
            model.FeaturedCars = Array.Empty<PublicCarListItemViewModel>();
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
