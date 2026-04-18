using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Showroom.Web.Configuration;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.ReportViewer)]
public class AdminController : Controller
{
    private readonly AdminCredentialsOptions _adminCredentials;
    private readonly IAuditLogService _auditLogService;
    private readonly IShowroomDataService _showroomDataService;

    public AdminController(
        IOptions<AdminCredentialsOptions> adminCredentials,
        IAuditLogService auditLogService,
        IShowroomDataService showroomDataService)
    {
        _adminCredentials = adminCredentials.Value;
        _auditLogService = auditLogService;
        _showroomDataService = showroomDataService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(new AdminLoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var accounts = _adminCredentials.GetAccounts();
        if (accounts.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Chua cau hinh tai khoan truy cap cho khu vuc quan tri.");
            return View(model);
        }

        var account = accounts.FirstOrDefault(candidate =>
            string.Equals(candidate.Username, model.Username, StringComparison.OrdinalIgnoreCase));

        var isValidUser = account is not null && MatchesPassword(account, model.Password);
        if (!isValidUser)
        {
            ModelState.AddModelError(string.Empty, "Ten dang nhap hoac mat khau quan tri khong chinh xac.");

            await _auditLogService.WriteAsync(
                new AuditLogEntry
                {
                    Username = model.Username.Trim(),
                    DisplayName = account?.DisplayName ?? model.Username.Trim(),
                    Role = account?.NormalizedRole ?? "Anonymous",
                    Action = "LOGIN_FAILED",
                    EntityType = "Authentication",
                    Description = "Dang nhap that bai vao khu vuc quan tri.",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                },
                cancellationToken);

            return View(model);
        }

        var authenticatedAccount = account!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, authenticatedAccount.DisplayName),
            new(ClaimTypes.NameIdentifier, authenticatedAccount.Username),
            new(ClaimTypes.Role, authenticatedAccount.NormalizedRole)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        await _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Username = authenticatedAccount.Username,
                DisplayName = authenticatedAccount.DisplayName,
                Role = authenticatedAccount.NormalizedRole,
                Action = "LOGIN_SUCCESS",
                EntityType = "Authentication",
                Description = $"Dang nhap thanh cong voi quyen {authenticatedAccount.NormalizedRole}.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            },
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await _showroomDataService.GetDashboardAsync(cancellationToken);
        return View(dashboard);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Username = User.GetUsername(),
                DisplayName = User.GetDisplayName(),
                Role = User.GetPrimaryRole(),
                Action = "LOGOUT",
                EntityType = "Authentication",
                Description = "Dang xuat khoi khu vuc quan tri.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            },
            cancellationToken);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private static bool MatchesPassword(ConfiguredAccountOptions account, string password)
    {
        if (!string.IsNullOrWhiteSpace(account.PasswordHash))
        {
            return PasswordHashing.VerifyPassword(password, account.PasswordHash);
        }

        return string.Equals(password, account.Password, StringComparison.Ordinal);
    }
}
