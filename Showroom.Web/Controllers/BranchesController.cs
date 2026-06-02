using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.CoreManager)]
public sealed class BranchesController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICoreManagementService _coreManagementService;

    public BranchesController(ICoreManagementService coreManagementService, IAuditLogService auditLogService)
    {
        _coreManagementService = coreManagementService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var branches = await _coreManagementService.GetBranchesAsync(cancellationToken);
            return View(branches);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return View(Array.Empty<BranchListItemViewModel>());
        }
    }

    [HttpGet]
    public IActionResult Create()
        => View(NewForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchFormViewModel model, CancellationToken cancellationToken)
    {
        model.StatusOptions = BranchStatusCatalog.GetSelectList(model.Status);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            Normalize(model);
            await _coreManagementService.CreateBranchAsync(model, cancellationToken);
            await WriteAuditAsync("BRANCH_CREATED", "Branch", null, $"Đã thêm chi nhánh '{model.BranchCode}'.", cancellationToken);

            SetStatus("Đã thêm chi nhánh.", "success");
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
        var model = await _coreManagementService.GetBranchAsync(id, cancellationToken);
        if (model is null)
        {
            SetStatus("Không tìm thấy chi nhánh cần sửa.", "warning");
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BranchFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        model.StatusOptions = BranchStatusCatalog.GetSelectList(model.Status);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            Normalize(model);
            var updated = await _coreManagementService.UpdateBranchAsync(model, cancellationToken);
            if (!updated)
            {
                SetStatus("Không tìm thấy chi nhánh cần cập nhật.", "warning");
                return RedirectToAction(nameof(Index));
            }

            await WriteAuditAsync("BRANCH_UPDATED", "Branch", model.Id, $"Đã cập nhật chi nhánh '{model.BranchCode}'.", cancellationToken);
            SetStatus("Đã cập nhật chi nhánh.", "success");
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
            var deleted = await _coreManagementService.DeleteBranchAsync(id, cancellationToken);
            if (deleted)
            {
                await WriteAuditAsync("BRANCH_DELETED", "Branch", id, $"Đã xoá chi nhánh có mã hệ thống {id}.", cancellationToken);
            }

            SetStatus(deleted ? "Đã xoá chi nhánh." : "Không tìm thấy chi nhánh cần xoá.", deleted ? "success" : "warning");
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
        }

        return RedirectToAction(nameof(Index));
    }

    private static BranchFormViewModel NewForm()
        => new()
        {
            Status = BranchStatusCatalog.Active,
            StatusOptions = BranchStatusCatalog.GetSelectList(BranchStatusCatalog.Active)
        };

    private static void Normalize(BranchFormViewModel model)
    {
        model.BranchCode = model.BranchCode.Trim();
        model.Name = model.Name.Trim();
        model.Status = BranchStatusCatalog.Normalize(model.Status);
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
