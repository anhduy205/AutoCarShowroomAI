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
            ModelState.AddModelError(nameof(model.Password), "Mật khẩu không được để trống.");
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
                $"Đã tạo tài khoản nhân viên '{username}'.",
                cancellationToken);

            TempData["StatusMessage"] = "Đã tạo tài khoản nhân viên.";
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
            TempData["StatusMessage"] = "Không tìm thấy tài khoản nhân viên.";
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
            ModelState.AddModelError(string.Empty, "Bạn không thể tự hạ quyền của chính mình.");
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
                TempData["StatusMessage"] = "Không tìm thấy tài khoản nhân viên de cap nhat.";
                TempData["StatusType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            await WriteAuditAsync(
                "STAFF_UPDATED",
                entityId: model.Id,
                $"Đã cập nhật tài khoản nhân viên '{model.Username.Trim()}'.",
                cancellationToken);

            TempData["StatusMessage"] = "Đã cập nhật tài khoản nhân viên.";
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
            TempData["StatusMessage"] = "Không tìm thấy tài khoản nhân viên.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        if (string.Equals(User.GetUsername(), user.Username, StringComparison.OrdinalIgnoreCase))
        {
            TempData["StatusMessage"] = "Bạn không thể tự xoá tài khoản đang đăng nhập.";
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
            TempData["StatusMessage"] = "Không tìm thấy tài khoản nhân viên.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        if (string.Equals(User.GetUsername(), user.Username, StringComparison.OrdinalIgnoreCase))
        {
            TempData["StatusMessage"] = "Bạn không thể tự xoá tài khoản đang đăng nhập.";
            TempData["StatusType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        var deleted = await _staffUsers.DeleteStaffUserAsync(id, cancellationToken);
        if (deleted)
        {
            await WriteAuditAsync(
                "STAFF_DELETED",
                entityId: id,
                $"Đã xoá tài khoản nhân viên '{user.Username}'.",
                cancellationToken);

            TempData["StatusMessage"] = "Đã xoá tài khoản nhân viên.";
            TempData["StatusType"] = "success";
        }
        else
        {
            TempData["StatusMessage"] = "Không thể xoá tài khoản nhân viên.";
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
