using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Models;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize]
public class BrandsController : Controller
{
    private readonly IInventoryManagementService _inventoryManagementService;

    public BrandsController(IInventoryManagementService inventoryManagementService)
    {
        _inventoryManagementService = inventoryManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _inventoryManagementService.GetBrandsAsync(cancellationToken);
            return View(brands);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return View(Array.Empty<BrandListItemViewModel>());
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BrandFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BrandFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _inventoryManagementService.CreateBrandAsync(model, cancellationToken);
            SetStatus("Đã thêm hãng xe mới.", "success");
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _inventoryManagementService.GetBrandAsync(id, cancellationToken);
            if (model is null)
            {
                SetStatus("Không tìm thấy hãng xe cần sửa.", "warning");
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
    public async Task<IActionResult> Edit(int id, BrandFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updated = await _inventoryManagementService.UpdateBrandAsync(model, cancellationToken);
            if (!updated)
            {
                SetStatus("Không tìm thấy hãng xe cần cập nhật.", "warning");
                return RedirectToAction(nameof(Index));
            }

            SetStatus("Đã cập nhật hãng xe.", "success");
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _inventoryManagementService.DeleteBrandAsync(id, cancellationToken);
            SetStatus(deleted ? "Đã xoá hãng xe." : "Không tìm thấy hãng xe cần xoá.", deleted ? "success" : "warning");
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
        }

        return RedirectToAction(nameof(Index));
    }

    private void SetStatus(string message, string type)
    {
        TempData["StatusMessage"] = message;
        TempData["StatusType"] = type;
    }
}
