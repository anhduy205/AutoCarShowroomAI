using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.CatalogManager)]
public class CarsController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly IInventoryManagementService _inventoryManagementService;

    public CarsController(
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
                SetStatus("Hay tao it nhat mot hang xe truoc khi them xe.", "warning");
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
            model.Name = model.Name.Trim();
            await _inventoryManagementService.CreateCarAsync(model, cancellationToken);
            await WriteAuditAsync(
                "CAR_CREATED",
                "Car",
                entityId: null,
                $"Da them xe '{model.Name.Trim()}'.",
                cancellationToken);

            SetStatus("Da them xe moi.", "success");
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
                SetStatus("Khong tim thay xe can sua.", "warning");
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
            model.Name = model.Name.Trim();
            var updated = await _inventoryManagementService.UpdateCarAsync(model, cancellationToken);
            if (!updated)
            {
                SetStatus("Khong tim thay xe can cap nhat.", "warning");
                return RedirectToAction(nameof(Index));
            }

            await WriteAuditAsync(
                "CAR_UPDATED",
                "Car",
                model.Id,
                $"Da cap nhat xe '{model.Name.Trim()}'.",
                cancellationToken);

            SetStatus("Da cap nhat thong tin xe.", "success");
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
            if (deleted)
            {
                await WriteAuditAsync(
                    "CAR_DELETED",
                    "Car",
                    id,
                    $"Da xoa xe co ma {id}.",
                    cancellationToken);
            }

            SetStatus(deleted ? "Da xoa xe." : "Khong tim thay xe can xoa.", deleted ? "success" : "warning");
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
