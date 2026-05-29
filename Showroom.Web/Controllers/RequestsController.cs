using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Showroom.Web.Models;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[EnableRateLimiting("PublicBrowse")]
public sealed class RequestsController : Controller
{
    private readonly ICustomerRequestService _customerRequestService;

    public RequestsController(ICustomerRequestService customerRequestService)
    {
        _customerRequestService = customerRequestService;
    }

    [HttpGet("/requests/create")]
    public async Task<IActionResult> Create(
        [FromQuery] int? carId,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var model = await _customerRequestService.GetNewRequestAsync(carId, type, cancellationToken);
        return View(model);
    }

    [HttpPost("/requests/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerRequestFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var requestId = await _customerRequestService.CreateRequestAsync(model, cancellationToken);
            TempData["StatusMessage"] = $"Đã gửi yêu cầu #{requestId}. Showroom sẽ thông báo qua email hoặc số điện thoại sau khi quản trị xác nhận.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Thanks), new { id = requestId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpGet("/requests/thanks/{id:int}")]
    public IActionResult Thanks(int id)
    {
        ViewData["RequestId"] = id;
        return View();
    }

    private async Task PopulateOptionsSafelyAsync(CustomerRequestFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _customerRequestService.PopulateOptionsAsync(model, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
    }
}
