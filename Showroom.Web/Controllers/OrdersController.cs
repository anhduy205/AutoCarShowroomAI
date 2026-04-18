using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.OrderManager)]
public class OrdersController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly IOrderManagementService _orderManagementService;

    public OrdersController(
        IOrderManagementService orderManagementService,
        IAuditLogService auditLogService)
    {
        _orderManagementService = orderManagementService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _orderManagementService.GetOrdersAsync(cancellationToken);
            return View(orders);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return View(Array.Empty<OrderListItemViewModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        try
        {
            var model = await _orderManagementService.GetNewOrderAsync(cancellationToken);
            if (model.CarOptions.Count == 0)
            {
                SetStatus("Hay tao it nhat mot xe truoc khi lap don hang.", "warning");
                return RedirectToAction("Create", "Cars");
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
    public async Task<IActionResult> Create(OrderFormViewModel model, CancellationToken cancellationToken)
    {
        NormalizeItems(model);
        ValidateItems(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var orderId = await _orderManagementService.CreateOrderAsync(model, cancellationToken);
            await WriteAuditAsync(
                "ORDER_CREATED",
                "Order",
                orderId,
                $"Da tao don hang cho khach '{model.CustomerName.Trim()}' voi trang thai '{model.Status}'.",
                cancellationToken);

            SetStatus("Da tao don hang moi.", "success");
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
            var model = await _orderManagementService.GetOrderAsync(id, cancellationToken);
            if (model is null)
            {
                SetStatus("Khong tim thay don hang can sua.", "warning");
                return RedirectToAction(nameof(Index));
            }

            EnsureAtLeastOneItem(model);
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
    public async Task<IActionResult> Edit(int id, OrderFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        NormalizeItems(model);
        ValidateItems(model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsSafelyAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var updated = await _orderManagementService.UpdateOrderAsync(model, cancellationToken);
            if (!updated)
            {
                SetStatus("Khong tim thay don hang can cap nhat.", "warning");
                return RedirectToAction(nameof(Index));
            }

            await WriteAuditAsync(
                "ORDER_UPDATED",
                "Order",
                model.Id,
                $"Da cap nhat don hang cua khach '{model.CustomerName.Trim()}' sang trang thai '{model.Status}'.",
                cancellationToken);

            SetStatus("Da cap nhat don hang.", "success");
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
            var deleted = await _orderManagementService.DeleteOrderAsync(id, cancellationToken);
            if (deleted)
            {
                await WriteAuditAsync(
                    "ORDER_DELETED",
                    "Order",
                    id,
                    $"Da xoa don hang co ma {id}.",
                    cancellationToken);
            }

            SetStatus(deleted ? "Da xoa don hang." : "Khong tim thay don hang can xoa.", deleted ? "success" : "warning");
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsSafelyAsync(OrderFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _orderManagementService.PopulateCarOptionsAsync(model, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        EnsureAtLeastOneItem(model);
    }

    private static void NormalizeItems(OrderFormViewModel model)
    {
        model.Items = model.Items
            .Where(item => item.CarId > 0 || item.Quantity > 0)
            .Select(item => new OrderFormItemViewModel
            {
                CarId = item.CarId,
                Quantity = item.Quantity
            })
            .ToList();
    }

    private void ValidateItems(OrderFormViewModel model)
    {
        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui long them it nhat mot xe vao don hang.");
            return;
        }

        if (model.Items.Any(item => item.CarId <= 0))
        {
            ModelState.AddModelError(string.Empty, "Moi dong trong don hang deu phai chon xe hop le.");
        }

        if (model.Items.Any(item => item.Quantity <= 0))
        {
            ModelState.AddModelError(string.Empty, "So luong moi xe trong don hang phai lon hon 0.");
        }

        var duplicatedCarIds = model.Items
            .GroupBy(item => item.CarId)
            .Where(group => group.Key > 0 && group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicatedCarIds.Length > 0)
        {
            ModelState.AddModelError(string.Empty, "Moi xe chi nen xuat hien mot lan trong don hang.");
        }
    }

    private static void EnsureAtLeastOneItem(OrderFormViewModel model)
    {
        if (model.Items.Count == 0)
        {
            model.Items.Add(new OrderFormItemViewModel());
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
