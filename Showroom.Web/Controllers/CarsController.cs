using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Models;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize]
public class CarsController : Controller
{
    private readonly IInventoryManagementService _inventoryManagementService;

    public CarsController(IInventoryManagementService inventoryManagementService)
    {
        _inventoryManagementService = inventoryManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var cars = await _inventoryManagementService.GetCarsAsync(cancellationToken);
            return View(cars);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return View(Array.Empty<CarListItemViewModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        try
        {
            var model = await _inventoryManagementService.GetNewCarAsync(cancellationToken);
            if (model.BrandOptions.Count == 0)
            {
                SetStatus("Hãy tạo ít nhất một hãng xe trước khi thêm xe.", "warning");
                return RedirectToAction("Create", "Brands");
            }

            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            await _inventoryManagementService.CreateCarAsync(model, cancellationToken);
            SetStatus("Đã thêm xe mới.", "success");
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _inventoryManagementService.GetCarAsync(id, cancellationToken);
            if (model is null)
            {
                SetStatus("Không tìm thấy xe cần sửa.", "warning");
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CarFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var updated = await _inventoryManagementService.UpdateCarAsync(model, cancellationToken);
            if (!updated)
            {
                SetStatus("Không tìm thấy xe cần cập nhật.", "warning");
                return RedirectToAction(nameof(Index));
            }

            SetStatus("Đã cập nhật thông tin xe.", "success");
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _inventoryManagementService.DeleteCarAsync(id, cancellationToken);
            SetStatus(deleted ? "Đã xoá xe." : "Không tìm thấy xe cần xoá.", deleted ? "success" : "warning");
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsSafelyAsync(CarFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _inventoryManagementService.PopulateBrandOptionsAsync(model, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
    }

    private void SetStatus(string message, string type)
    {
        TempData["StatusMessage"] = message;
        TempData["StatusType"] = type;
    }
}
