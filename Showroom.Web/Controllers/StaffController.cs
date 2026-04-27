using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.CatalogManager)]
public sealed class StaffController : Controller
{
    private readonly IStaffUserManagementService _staffUsers;
    private readonly IAuditLogService _auditLogService;

    public StaffController(IStaffUserManagementService staffUsers, IAuditLogService auditLogService)
    {
        _staffUsers = staffUsers;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _staffUsers.GetStaffUsersAsync(cancellationToken);
        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
        => View(new StaffUserFormViewModel { RequiresPassword = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffUserFormViewModel model, CancellationToken cancellationToken)
    {
        model.RequiresPassword = true;
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Mat khau khong duoc de trong.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var username = model.Username.Trim();
            var displayName = model.DisplayName.Trim();

            var id = await _staffUsers.CreateStaffUserAsync(
                new StaffUserCreateRequest
                {
                    Username = username,
                    DisplayName = displayName,
                    Role = model.Role,
                    PasswordHash = PasswordHashing.HashPassword(model.Password!.Trim())
                },
                cancellationToken);

            await WriteAuditAsync(
                "STAFF_CREATED",
                entityId: id,
                $"Da tao tai khoan nhan vien '{username}'.",
                cancellationToken);

            TempData["StatusMessage"] = "Da tao tai khoan nhan vien.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (FriendlyOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var user = await _staffUsers.GetStaffUserAsync(id, cancellationToken);
        if (user is null)
        {
            TempData["StatusMessage"] = "Khong tim thay tai khoan nhan vien.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        return View(new StaffUserFormViewModel
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            RequiresPassword = false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StaffUserFormViewModel model, CancellationToken cancellationToken)
    {
        model.RequiresPassword = false;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.Equals(User.GetUsername(), model.Username.Trim(), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(model.Role, ShowroomRoles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Ban khong the tu ha quyen cua chinh minh.");
            return View(model);
        }

        try
        {
            var passwordHash = string.IsNullOrWhiteSpace(model.Password)
                ? null
                : PasswordHashing.HashPassword(model.Password.Trim());

            var updated = await _staffUsers.UpdateStaffUserAsync(
                new StaffUserUpdateRequest
                {
                    Id = model.Id,
                    Username = model.Username.Trim(),
                    DisplayName = model.DisplayName.Trim(),
                    Role = model.Role,
                    PasswordHash = passwordHash
                },
                cancellationToken);

            if (!updated)
            {
                TempData["StatusMessage"] = "Khong tim thay tai khoan nhan vien de cap nhat.";
                TempData["StatusType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            await WriteAuditAsync(
                "STAFF_UPDATED",
                entityId: model.Id,
                $"Da cap nhat tai khoan nhan vien '{model.Username.Trim()}'.",
                cancellationToken);

            TempData["StatusMessage"] = "Da cap nhat tai khoan nhan vien.";
            TempData["StatusType"] = "success";
            return RedirectToAction(nameof(Edit), new { id = model.Id });
        }
        catch (FriendlyOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var user = await _staffUsers.GetStaffUserAsync(id, cancellationToken);
        if (user is null)
        {
            TempData["StatusMessage"] = "Khong tim thay tai khoan nhan vien.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        if (string.Equals(User.GetUsername(), user.Username, StringComparison.OrdinalIgnoreCase))
        {
            TempData["StatusMessage"] = "Ban khong the tu xoa tai khoan dang dang nhap.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var user = await _staffUsers.GetStaffUserAsync(id, cancellationToken);
        if (user is null)
        {
            TempData["StatusMessage"] = "Khong tim thay tai khoan nhan vien.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        if (string.Equals(User.GetUsername(), user.Username, StringComparison.OrdinalIgnoreCase))
        {
            TempData["StatusMessage"] = "Ban khong the tu xoa tai khoan dang dang nhap.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        var deleted = await _staffUsers.DeleteStaffUserAsync(id, cancellationToken);
        if (deleted)
        {
            await WriteAuditAsync(
                "STAFF_DELETED",
                entityId: id,
                $"Da xoa tai khoan nhan vien '{user.Username}'.",
                cancellationToken);

            TempData["StatusMessage"] = "Da xoa tai khoan nhan vien.";
            TempData["StatusType"] = "success";
        }
        else
        {
            TempData["StatusMessage"] = "Khong the xoa tai khoan nhan vien.";
            TempData["StatusType"] = "warning";
        }

        return RedirectToAction(nameof(Index));
    }

    private Task WriteAuditAsync(string action, int? entityId, string description, CancellationToken cancellationToken)
        => _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Username = User.GetUsername(),
                DisplayName = User.GetDisplayName(),
                Role = User.GetPrimaryRole(),
                Action = action,
                EntityType = "StaffUser",
                EntityId = entityId,
                Description = description,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            },
            cancellationToken);
}
