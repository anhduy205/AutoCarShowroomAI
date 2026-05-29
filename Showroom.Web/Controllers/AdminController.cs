using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    private readonly ILogger<AdminController> _logger;
    private readonly AdminCredentialsOptions _adminCredentials;
    private readonly IAdminLoginLockoutService _adminLoginLockoutService;
    private readonly IAuditLogService _auditLogService;
    private readonly IShowroomDataService _showroomDataService;
    private readonly IStaffUserManagementService _staffUsers;

    public AdminController(
        IOptions<AdminCredentialsOptions> adminCredentials,
        IAdminLoginLockoutService adminLoginLockoutService,
        IAuditLogService auditLogService,
        IShowroomDataService showroomDataService,
        IStaffUserManagementService staffUsers,
        ILogger<AdminController> logger)
    {
        _adminCredentials = adminCredentials.Value;
        _adminLoginLockoutService = adminLoginLockoutService;
        _auditLogService = auditLogService;
        _showroomDataService = showroomDataService;
        _staffUsers = staffUsers;
        _logger = logger;
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
    [EnableRateLimiting(AdminLoginProtectionOptions.RateLimitPolicyName)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model, CancellationToken cancellationToken)
    {
        model.Username = model.Username.Trim();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ipAddress = GetClientIpAddress();
        var loginStatus = _adminLoginLockoutService.GetStatus(model.Username, ipAddress);
        if (loginStatus.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, BuildLockoutMessage(loginStatus));
            await WriteAuthenticationAuditAsync(
                model.Username,
                model.Username,
                "Anonymous",
                "LOGIN_LOCKED_OUT",
                "Đăng nhập bi tu choi do tai khoan tam khoa.",
                ipAddress,
                cancellationToken);
            return View(model);
        }

        var authenticatedAccount = await TryAuthenticateAsync(model.Username, model.Password, cancellationToken);

        var isValidUser = authenticatedAccount is not null;
        if (!isValidUser)
        {
            var failureStatus = _adminLoginLockoutService.RegisterFailure(model.Username, ipAddress);
            ModelState.AddModelError(
                string.Empty,
                failureStatus.IsLockedOut
                    ? BuildLockoutMessage(failureStatus)
                    : "Tên đăng nhập hoặc mật khẩu quản trị không chính xác.");

            await WriteAuthenticationAuditAsync(
                model.Username,
                authenticatedAccount?.DisplayName ?? model.Username,
                authenticatedAccount?.NormalizedRole ?? "Anonymous",
                failureStatus.IsLockedOut ? "LOGIN_LOCKED_OUT" : "LOGIN_FAILED",
                failureStatus.IsLockedOut
                    ? "Đăng nhập bị khóa tạm thời sau quá nhiều lần thất bại."
                    : "Đăng nhập thất bại vào khu vực quản trị.",
                ipAddress,
                cancellationToken);

            return View(model);
        }

        _adminLoginLockoutService.Reset(authenticatedAccount!.Username, ipAddress);

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

        await WriteAuthenticationAuditAsync(
            authenticatedAccount.Username,
            authenticatedAccount.DisplayName,
            authenticatedAccount.NormalizedRole,
            "LOGIN_SUCCESS",
            $"Đăng nhập thanh cong voi quyen {authenticatedAccount.NormalizedRole}.",
            ipAddress,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<ConfiguredAccountOptions?> TryAuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var accounts = _adminCredentials.GetAccounts();
        var configured = accounts.FirstOrDefault(candidate =>
            string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase));

        if (configured is not null && MatchesPassword(configured, password))
        {
            return configured;
        }

        try
        {
            var staffUser = await _staffUsers.FindByUsernameAsync(username, cancellationToken);
            if (staffUser is null)
            {
                return null;
            }

            if (!PasswordHashing.VerifyPassword(password, staffUser.PasswordHash))
            {
                return null;
            }

            return new ConfiguredAccountOptions
            {
                Username = staffUser.Username,
                PasswordHash = staffUser.PasswordHash,
                DisplayName = staffUser.DisplayName,
                Role = staffUser.Role
            };
        }
        catch (FriendlyOperationException ex)
        {
            _logger.LogWarning(ex, "Could not authenticate using StaffUsers table.");
            return null;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? salesFrom, DateOnly? salesTo, CancellationToken cancellationToken)
    {
        if (salesFrom is not null && salesTo is not null && salesFrom > salesTo)
        {
            (salesFrom, salesTo) = (salesTo, salesFrom);
        }

        var dashboard = await _showroomDataService.GetDashboardAsync(salesFrom, salesTo, cancellationToken);
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
        await WriteAuthenticationAuditAsync(
            User.GetUsername(),
            User.GetDisplayName(),
            User.GetPrimaryRole(),
            "LOGOUT",
            "Đăng xuất khỏi khu vực quản trị.",
            GetClientIpAddress(),
            cancellationToken);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private static string BuildLockoutMessage(AdminLoginAttemptStatus loginStatus)
    {
        var retryAfter = loginStatus.RetryAfter ?? TimeSpan.Zero;
        var totalMinutes = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));
        return $"Tài khoản tạm khóa sau quá nhiều lần đăng nhập sai. Hãy thử lại sau {totalMinutes} phút.";
    }

    private static bool MatchesPassword(ConfiguredAccountOptions account, string password)
    {
        if (!string.IsNullOrWhiteSpace(account.PasswordHash))
        {
            return PasswordHashing.VerifyPassword(password, account.PasswordHash);
        }

        return string.Equals(password, account.Password, StringComparison.Ordinal);
    }

    private string GetClientIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    private Task WriteAuthenticationAuditAsync(
        string username,
        string displayName,
        string role,
        string action,
        string description,
        string ipAddress,
        CancellationToken cancellationToken)
        => _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Username = username,
                DisplayName = displayName,
                Role = role,
                Action = action,
                EntityType = "Authentication",
                Description = description,
                IpAddress = ipAddress
            },
            cancellationToken);
}
