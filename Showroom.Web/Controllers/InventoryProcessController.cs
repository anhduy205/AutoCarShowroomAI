using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.InventoryManager)]
[Route("admin/inventory")]
public sealed class InventoryProcessController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly IInventoryProcessService _inventoryProcessService;

    public InventoryProcessController(
        IInventoryProcessService inventoryProcessService,
        IAuditLogService auditLogService)
    {
        _inventoryProcessService = inventoryProcessService;
        _auditLogService = auditLogService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var model = await _inventoryProcessService.GetDashboardAsync(cancellationToken);
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return View(new InventoryProcessDashboardViewModel());
        }
    }

    [HttpPost("models")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateVehicleModel([Bind(Prefix = "VehicleModelForm")] VehicleModelFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.CreateVehicleModelAsync(model, cancellationToken),
            "VEHICLE_MODEL_CREATED",
            "VehicleModel",
            $"Đã thêm mẫu xe '{model.ModelCode}'.",
            "Đã thêm mẫu xe.",
            "Thông tin mẫu xe chưa hợp lệ.",
            cancellationToken);

    [HttpPost("locations")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateLocation([Bind(Prefix = "LocationForm")] LocationFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.CreateLocationAsync(model, cancellationToken),
            "LOCATION_CREATED",
            "Location",
            $"Đã thêm vị trí kho '{model.LocationCode}'.",
            "Đã thêm vị trí kho.",
            "Thông tin vị trí chưa hợp lệ.",
            cancellationToken);

    [HttpPost("vehicles")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateNewVehicle([Bind(Prefix = "NewVehicleForm")] NewVehicleFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.CreateNewVehicleAsync(model, cancellationToken),
            "NEW_VEHICLE_CREATED",
            "NewVehicle",
            $"Đã thêm xe theo VIN '{model.Vin}'.",
            "Đã thêm xe theo VIN.",
            "Thông tin xe theo VIN chưa hợp lệ.",
            cancellationToken);

    [HttpPost("vehicles/move")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> MoveVehicle([Bind(Prefix = "VehicleMovementForm")] VehicleMovementFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.MoveVehicleAsync(model, cancellationToken),
            "VEHICLE_MOVED",
            "NewVehicle",
            $"Đã chuyển vị trí xe có mã hệ thống {model.NewVehicleId}.",
            "Đã chuyển vị trí xe.",
            "Thông tin chuyển vị trí chưa hợp lệ.",
            cancellationToken);

    [HttpPost("factory-orders")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateFactoryOrder([Bind(Prefix = "FactoryOrderForm")] FactoryOrderFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.CreateFactoryOrderAsync(model, cancellationToken),
            "FACTORY_ORDER_CREATED",
            "FactoryOrder",
            $"Đã tạo đơn nhà máy '{model.FactoryOrderNo}'.",
            "Đã tạo đơn nhà máy.",
            "Thông tin đơn nhà máy chưa hợp lệ.",
            cancellationToken);

    [HttpPost("parts")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreatePart([Bind(Prefix = "PartForm")] PartFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.CreatePartAsync(model, cancellationToken),
            "PART_CREATED",
            "Part",
            $"Đã thêm phụ tùng '{model.PartCode}'.",
            "Đã thêm phụ tùng.",
            "Thông tin phụ tùng chưa hợp lệ.",
            cancellationToken);

    [HttpPost("parts/adjust")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AdjustPartInventory([Bind(Prefix = "PartInventoryAdjustmentForm")] PartInventoryAdjustmentFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.AdjustPartInventoryAsync(model, cancellationToken),
            "PART_STOCK_ADJUSTED",
            "PartInventory",
            $"Đã điều chỉnh tồn phụ tùng có mã hệ thống {model.PartId}.",
            "Đã điều chỉnh tồn phụ tùng.",
            "Thông tin điều chỉnh tồn chưa hợp lệ.",
            cancellationToken);

    [HttpPost("part-purchase-orders")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreatePartPurchaseOrder([Bind(Prefix = "PartPurchaseOrderForm")] PartPurchaseOrderFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.CreatePartPurchaseOrderAsync(model, cancellationToken),
            "PART_PURCHASE_ORDER_CREATED",
            "PartPurchaseOrder",
            $"Đã tạo đơn phụ tùng '{model.PurchaseOrderNo}'.",
            "Đã tạo đơn phụ tùng.",
            "Thông tin đơn phụ tùng chưa hợp lệ.",
            cancellationToken);

    [HttpPost("part-purchase-orders/receive")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ReceivePartPurchaseOrderItem([Bind(Prefix = "PartPurchaseReceiptForm")] PartPurchaseReceiptFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _inventoryProcessService.ReceivePartPurchaseOrderItemAsync(model, cancellationToken),
            "PART_PURCHASE_RECEIVED",
            "PartPurchaseOrder",
            $"Đã nhận phụ tùng từ dòng đơn có mã hệ thống {model.PartPurchaseOrderItemId}.",
            "Đã nhận phụ tùng vào kho.",
            "Thông tin nhận phụ tùng chưa hợp lệ.",
            cancellationToken);

    private async Task<IActionResult> RunActionAsync(
        bool isValid,
        Func<Task> action,
        string auditAction,
        string entityType,
        string auditDescription,
        string successMessage,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        if (!isValid)
        {
            SetStatus(invalidMessage, "warning");
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await action();
            await WriteAuditAsync(auditAction, entityType, auditDescription, cancellationToken);
            SetStatus(successMessage, "success");
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

    private Task WriteAuditAsync(string action, string entityType, string description, CancellationToken cancellationToken)
        => _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Username = User.GetUsername(),
                DisplayName = User.GetDisplayName(),
                Role = User.GetPrimaryRole(),
                Action = action,
                EntityType = entityType,
                Description = description,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            },
            cancellationToken);
}
