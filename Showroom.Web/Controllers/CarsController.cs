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
    private readonly ICarImageStorageService _carImageStorageService;

    public CarsController(
        IInventoryManagementService inventoryManagementService,
        IAuditLogService auditLogService,
        ICarImageStorageService carImageStorageService)
    {
        _inventoryManagementService = inventoryManagementService;
        _auditLogService = auditLogService;
        _carImageStorageService = carImageStorageService;
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
    public async Task<IActionResult> Create(CarFormViewModel model, List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            model.Name = model.Name.Trim();
            var carId = await _inventoryManagementService.CreateCarAsync(model, cancellationToken);

            var updatedImageUrls = model.ImageUrls;
            if (files.Count > 0)
            {
                if (files.Count > 10)
                {
                    throw new InvalidOperationException("Chi co the tai toi da 10 anh trong mot lan.");
                }

                foreach (var file in files)
                {
                    var image = await _carImageStorageService.SaveAsync(carId, file, cancellationToken);
                    updatedImageUrls = AppendImageUrl(updatedImageUrls, image.ImageUrl);
                }

                await _inventoryManagementService.UpdateCarImageUrlsAsync(carId, updatedImageUrls, cancellationToken);
            }

            await WriteAuditAsync(
                "CAR_CREATED",
                "Car",
                entityId: carId,
                $"Da them xe '{model.Name.Trim()}'.",
                cancellationToken);

            SetStatus("Da them xe moi.", "success");
            return RedirectToAction(nameof(Edit), new { id = carId });
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

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _inventoryManagementService.GetCarDetailsAsync(id, cancellationToken);
            if (model is null)
            {
                SetStatus("Khong tim thay xe can xem.", "warning");
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
    public async Task<IActionResult> UploadImage(int id, List<IFormFile> files, CancellationToken cancellationToken)
    {
        try
        {
            var car = await _inventoryManagementService.GetCarAsync(id, cancellationToken);
            if (car is null)
            {
                SetStatus("Khong tim thay xe de tai anh.", "warning");
                return RedirectToAction(nameof(Index));
            }

            if (files.Count == 0)
            {
                SetStatus("Vui long chon it nhat 1 anh de tai len.", "warning");
                return RedirectToAction(nameof(Edit), new { id });
            }

            var updatedImageUrls = car.ImageUrls;
            var uploadedCount = 0;

            foreach (var file in files)
            {
                var result = await _carImageStorageService.SaveAsync(id, file, cancellationToken);
                updatedImageUrls = AppendImageUrl(updatedImageUrls, result.ImageUrl);
                uploadedCount++;
            }

            await _inventoryManagementService.UpdateCarImageUrlsAsync(id, updatedImageUrls, cancellationToken);

            await WriteAuditAsync(
                "CAR_IMAGE_UPLOADED",
                "Car",
                id,
                $"Da tai {uploadedCount} anh cho xe '{car.Name.Trim()}'.",
                cancellationToken);

            SetStatus($"Da tai {uploadedCount} anh len.", "success");
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, string imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            var car = await _inventoryManagementService.GetCarAsync(id, cancellationToken);
            if (car is null)
            {
                SetStatus("Khong tim thay xe de xoa anh.", "warning");
                return RedirectToAction(nameof(Index));
            }

            await _carImageStorageService.DeleteAsync(id, imageUrl, cancellationToken);
            var updatedImageUrls = RemoveImageUrl(car.ImageUrls, imageUrl);
            await _inventoryManagementService.UpdateCarImageUrlsAsync(id, updatedImageUrls, cancellationToken);

            await WriteAuditAsync(
                "CAR_IMAGE_DELETED",
                "Car",
                id,
                $"Da xoa anh cua xe '{car.Name.Trim()}'.",
                cancellationToken);

            SetStatus("Da xoa anh.", "success");
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    private static string AppendImageUrl(string? existing, string newUrl)
    {
        var lines = SplitLines(existing).ToList();
        if (!lines.Contains(newUrl, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add(newUrl);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string? RemoveImageUrl(string? existing, string urlToRemove)
    {
        var lines = SplitLines(existing)
            .Where(line => !string.Equals(line, urlToRemove, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> SplitLines(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line));
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
