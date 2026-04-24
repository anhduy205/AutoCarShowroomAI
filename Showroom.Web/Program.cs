using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Showroom.Web.Configuration;
using Showroom.Web.Security;
using Showroom.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var adminLoginProtectionOptions =
    builder.Configuration.GetSection(AdminLoginProtectionOptions.SectionName).Get<AdminLoginProtectionOptions>()
    ?? new AdminLoginProtectionOptions();

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.Configure<AdminCredentialsOptions>(
    builder.Configuration.GetSection(AdminCredentialsOptions.SectionName));
builder.Services.Configure<AdminLoginProtectionOptions>(
    builder.Configuration.GetSection(AdminLoginProtectionOptions.SectionName));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/AccessDenied";
        options.Cookie.Name = "Showroom.AdminAuth";
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        ShowroomPolicies.ReportViewer,
        policy => policy.RequireRole(ShowroomRoles.Administrator, ShowroomRoles.Staff));

    options.AddPolicy(
        ShowroomPolicies.CatalogManager,
        policy => policy.RequireRole(ShowroomRoles.Administrator));

    options.AddPolicy(
        ShowroomPolicies.OrderManager,
        policy => policy.RequireRole(ShowroomRoles.Administrator, ShowroomRoles.Staff));

    options.AddPolicy(
        ShowroomPolicies.AuditViewer,
        policy => policy.RequireRole(ShowroomRoles.Administrator));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Qua nhieu yeu cau dang nhap. Hay doi mot chut roi thu lai.",
            token);
    };

    options.AddPolicy(
        AdminLoginProtectionOptions.RateLimitPolicyName,
        httpContext =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                ipAddress,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, adminLoginProtectionOptions.LoginRequestsPerMinute),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
});
builder.Services.AddScoped<IShowroomDataService, SqlShowroomDataService>();
builder.Services.AddScoped<IInventoryManagementService, SqlInventoryManagementService>();
builder.Services.AddScoped<IOrderManagementService, SqlOrderManagementService>();
builder.Services.AddScoped<IAuditLogService, SqlAuditLogService>();
builder.Services.AddSingleton<IAdminLoginLockoutService, InMemoryAdminLoginLockoutService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program
{
}
