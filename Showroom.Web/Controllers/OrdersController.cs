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
                SetStatus("Hãy tạo ít nhất một xe trước khi lập đơn hàng.", "warning");
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
            model.CustomerName = model.CustomerName.Trim();
            model.Status = model.Status.Trim();
            var orderId = await _orderManagementService.CreateOrderAsync(model, cancellationToken);
            await WriteAuditAsync(
                "ORDER_CREATED",
                "Order",
                orderId,
                $"Đã tạo đơn hàng cho khách '{model.CustomerName.Trim()}' với trạng thái '{model.Status}'.",
                cancellationToken);

            SetStatus("Đã tạo đơn hàng mới.", "success");
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
                SetStatus("Không tìm thấy đơn hàng cần sửa.", "warning");
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

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _orderManagementService.GetOrderDetailsAsync(id, cancellationToken);
            if (model is null)
            {
                SetStatus("Không tìm thấy đơn hàng cần xem.", "warning");
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
            model.CustomerName = model.CustomerName.Trim();
            model.Status = model.Status.Trim();
            var updated = await _orderManagementService.UpdateOrderAsync(model, cancellationToken);
            if (!updated)
            {
                SetStatus("Không tìm thấy đơn hàng cần cập nhật.", "warning");
                return RedirectToAction(nameof(Index));
            }

            await WriteAuditAsync(
                "ORDER_UPDATED",
                "Order",
                model.Id,
                $"Đã cập nhật đơn hàng của khách '{model.CustomerName.Trim()}' sang trạng thái '{model.Status}'.",
                cancellationToken);

            SetStatus("Đã cập nhật đơn hàng.", "success");
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
                    $"Đã xoá đơn hàng có mã {id}.",
                    cancellationToken);
            }

            SetStatus(deleted ? "Đã xoá đơn hàng." : "Không tìm thấy đơn hàng cần xoá.", deleted ? "success" : "warning");
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
            ModelState.AddModelError(string.Empty, "Vui lòng thêm ít nhất một xe vào đơn hàng.");
            return;
        }

        if (model.Items.Any(item => item.CarId <= 0))
        {
            ModelState.AddModelError(string.Empty, "Mỗi dòng trong đơn hàng đều phải chọn xe hợp lệ.");
        }

        if (model.Items.Any(item => item.Quantity <= 0))
        {
            ModelState.AddModelError(string.Empty, "Số lượng mỗi xe trong đơn hàng phải lớn hơn 0.");
        }

        var duplicatedCarIds = model.Items
            .GroupBy(item => item.CarId)
            .Where(group => group.Key > 0 && group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicatedCarIds.Length > 0)
        {
            ModelState.AddModelError(string.Empty, "Mỗi xe chỉ nên xuất hiện một lần trong đơn hàng.");
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
