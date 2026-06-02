using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.CoreManager)]
public sealed class CustomersController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICoreManagementService _coreManagementService;

    public CustomersController(ICoreManagementService coreManagementService, IAuditLogService auditLogService)
    {
        _coreManagementService = coreManagementService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var customers = await _coreManagementService.GetCustomersAsync(cancellationToken);
            return View(customers);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return View(Array.Empty<CustomerListItemViewModel>());
        }
    }

    [HttpGet]
    public IActionResult Create()
        => View(NewForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model, CancellationToken cancellationToken)
    {
        model.CustomerTypeOptions = CustomerTypeCatalog.GetSelectList(model.CustomerType);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            Normalize(model);
            await _coreManagementService.CreateCustomerAsync(model, cancellationToken);
            await WriteAuditAsync("CUSTOMER_CREATED", "Customer", null, $"Đã thêm khách hàng '{model.CustomerCode}'.", cancellationToken);

            SetStatus("Đã thêm khách hàng.", "success");
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
        var model = await _coreManagementService.GetCustomerAsync(id, cancellationToken);
        if (model is null)
        {
            SetStatus("Không tìm thấy khách hàng cần sửa.", "warning");
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        model.CustomerTypeOptions = CustomerTypeCatalog.GetSelectList(model.CustomerType);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            Normalize(model);
            var updated = await _coreManagementService.UpdateCustomerAsync(model, cancellationToken);
            if (!updated)
            {
                SetStatus("Không tìm thấy khách hàng cần cập nhật.", "warning");
                return RedirectToAction(nameof(Index));
            }

            await WriteAuditAsync("CUSTOMER_UPDATED", "Customer", model.Id, $"Đã cập nhật khách hàng '{model.CustomerCode}'.", cancellationToken);
            SetStatus("Đã cập nhật khách hàng.", "success");
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
            var deleted = await _coreManagementService.DeleteCustomerAsync(id, cancellationToken);
            if (deleted)
            {
                await WriteAuditAsync("CUSTOMER_DELETED", "Customer", id, $"Đã xoá khách hàng có mã hệ thống {id}.", cancellationToken);
            }

            SetStatus(deleted ? "Đã xoá khách hàng." : "Không tìm thấy khách hàng cần xoá.", deleted ? "success" : "warning");
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
        }

        return RedirectToAction(nameof(Index));
    }

    private static CustomerFormViewModel NewForm()
        => new()
        {
            CustomerType = CustomerTypeCatalog.Individual,
            CustomerTypeOptions = CustomerTypeCatalog.GetSelectList(CustomerTypeCatalog.Individual)
        };

    private static void Normalize(CustomerFormViewModel model)
    {
        model.CustomerCode = model.CustomerCode.Trim();
        model.FullName = model.FullName.Trim();
        model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        model.TaxCode = string.IsNullOrWhiteSpace(model.TaxCode) ? null : model.TaxCode.Trim();
        model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        model.CustomerType = CustomerTypeCatalog.Normalize(model.CustomerType);
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
