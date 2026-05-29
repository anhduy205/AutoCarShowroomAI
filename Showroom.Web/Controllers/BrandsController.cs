using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.CatalogManager)]
public class BrandsController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly IInventoryManagementService _inventoryManagementService;

    public BrandsController(
        IInventoryManagementService inventoryManagementService,
        IAuditLogService auditLogService)
    {
        _inventoryManagementService = inventoryManagementService;
        _auditLogService = auditLogService;
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
            model.Name = model.Name.Trim();
            await _inventoryManagementService.CreateBrandAsync(model, cancellationToken);
            await WriteAuditAsync(
                "BRAND_CREATED",
                "Brand",
                entityId: null,
                $"Đã thêm hãng xe '{model.Name.Trim()}'.",
                cancellationToken);

            SetStatus("Đã thêm hãng xe moi.", "success");
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
            model.Name = model.Name.Trim();
            var updated = await _inventoryManagementService.UpdateBrandAsync(model, cancellationToken);
            if (!updated)
            {
                SetStatus("Không tìm thấy hãng xe cần cập nhật.", "warning");
                return RedirectToAction(nameof(Index));
            }

            await WriteAuditAsync(
                "BRAND_UPDATED",
                "Brand",
                model.Id,
                $"Đã cập nhật hãng xe '{model.Name.Trim()}'.",
                cancellationToken);

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
            if (deleted)
            {
                await WriteAuditAsync(
                    "BRAND_DELETED",
                    "Brand",
                    id,
                    $"Đã xoá hãng xe có mã {id}.",
                    cancellationToken);
            }

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

    private Task WriteAuditAsync(string action, string entityType, int? entityId, string description, CancellationToken cancellationToken)
        => _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Username = User.GetUsername(),
                DisplayName = User.GetDisplayName(),
                Role = User.GetPrimaryRole(),
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            },
            cancellationToken);
}
