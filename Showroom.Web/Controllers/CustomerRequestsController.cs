using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.OrderManager)]
public sealed class CustomerRequestsController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICustomerRequestService _customerRequestService;

    public CustomerRequestsController(
        ICustomerRequestService customerRequestService,
        IAuditLogService auditLogService)
    {
        _customerRequestService = customerRequestService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _customerRequestService.GetRequestsAsync(cancellationToken);
            return View(requests);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return View(Array.Empty<CustomerRequestListItemViewModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var request = await _customerRequestService.GetRequestAsync(id, cancellationToken);
        if (request is null)
        {
            SetStatus("Không tìm thấy yêu cầu khách hàng.", "warning");
            return RedirectToAction(nameof(Index));
        }

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        var result = await _customerRequestService.ConfirmAsync(id, cancellationToken);
        if (result.Success)
        {
            await WriteAuditAsync(
                "CUSTOMER_REQUEST_CONFIRMED",
                "CustomerRequest",
                id,
                $"Đã xác nhận yêu cầu khách hàng và thông báo qua {result.NotificationChannel}.",
                cancellationToken);
        }

        SetStatus(result.Message, result.Success ? "success" : "warning");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var cancelled = await _customerRequestService.CancelAsync(id, cancellationToken);
        if (cancelled)
        {
            await WriteAuditAsync(
                "CUSTOMER_REQUEST_CANCELLED",
                "CustomerRequest",
                id,
                "Đã hủy yêu cầu khách hàng.",
                cancellationToken);
        }

        SetStatus(cancelled ? "Đã hủy yêu cầu." : "Không tìm thấy yêu cầu đang chờ xử lý.", cancelled ? "success" : "warning");
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
