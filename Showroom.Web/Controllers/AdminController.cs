using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Showroom.Web.Configuration;
using Showroom.Web.Models;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly AdminCredentialsOptions _adminCredentials;
    private readonly IShowroomDataService _showroomDataService;

    public AdminController(
        IOptions<AdminCredentialsOptions> adminCredentials,
        IShowroomDataService showroomDataService)
    {
        _adminCredentials = adminCredentials.Value;
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
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var isValidUser =
            string.Equals(model.Username, _adminCredentials.Username, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(model.Password, _adminCredentials.Password, StringComparison.Ordinal);

        if (!isValidUser)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu quản trị không chính xác.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _adminCredentials.DisplayName),
            new(ClaimTypes.NameIdentifier, model.Username),
            new(ClaimTypes.Role, "Administrator")
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
