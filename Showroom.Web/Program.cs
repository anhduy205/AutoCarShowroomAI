using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Filters;
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

builder.Services.AddScoped<FriendlyOperationExceptionFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<FriendlyOperationExceptionFilter>();
});
builder.Services.AddMemoryCache();
builder.Services.Configure<AdminCredentialsOptions>(
    builder.Configuration.GetSection(AdminCredentialsOptions.SectionName));
builder.Services.Configure<AdminLoginProtectionOptions>(
    builder.Configuration.GetSection(AdminLoginProtectionOptions.SectionName));
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));
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
        var httpContext = context.HttpContext;
        if (httpContext.Request.Path.StartsWithSegments("/api/chat", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Response.ContentType = "application/problem+json; charset=utf-8";
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Qua nhieu yeu cau chatbot. Hay doi mot chut roi thu lai."
                },
                token);

            return;
        }

        if (httpContext.Request.Path.StartsWithSegments("/cars", StringComparison.OrdinalIgnoreCase) ||
            httpContext.Request.Path.Equals("/", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Response.ContentType = "text/plain; charset=utf-8";
            await httpContext.Response.WriteAsync(
                "Qua nhieu yeu cau. Hay doi mot chut roi thu lai.",
                token);
            return;
        }

        httpContext.Response.ContentType = "text/plain; charset=utf-8";
        await httpContext.Response.WriteAsync(
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

    options.AddPolicy(
        "ChatApi",
        httpContext =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetTokenBucketLimiter(
                ipAddress,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 20,
                    TokensPerPeriod = 20,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });

    options.AddPolicy(
        "PublicBrowse",
        httpContext =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                ipAddress,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
});
builder.Services.AddScoped<IShowroomDataService, SqlShowroomDataService>();
builder.Services.AddScoped<IReportService, SqlReportService>();
builder.Services.AddScoped<IInventoryManagementService, SqlInventoryManagementService>();
builder.Services.AddScoped<IOrderManagementService, SqlOrderManagementService>();
builder.Services.AddScoped<IAuditLogService, SqlAuditLogService>();
builder.Services.AddScoped<IStaffUserManagementService, SqlStaffUserManagementService>();
builder.Services.AddHttpClient<OpenAiChatService>();
builder.Services.AddScoped<IAiChatService, AiCarAdvisorChatService>();
builder.Services.AddScoped<ICarImageStorageService, CarImageStorageService>();
builder.Services.AddSingleton<IAdminLoginLockoutService, InMemoryAdminLoginLockoutService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    await next();
});

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
