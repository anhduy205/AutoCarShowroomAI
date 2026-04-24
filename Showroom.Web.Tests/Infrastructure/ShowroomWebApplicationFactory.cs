using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Showroom.Web.Security;

namespace Showroom.Web.Tests.Infrastructure;

internal sealed class ShowroomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string DefaultPassword = "Admin@123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ShowroomDb"] = string.Empty,
                ["AdminCredentials:Accounts:0:Username"] = "admin",
                ["AdminCredentials:Accounts:0:PasswordHash"] = PasswordHashing.HashPassword(DefaultPassword),
                ["AdminCredentials:Accounts:0:DisplayName"] = "Test Admin",
                ["AdminCredentials:Accounts:0:Role"] = "Administrator",
                ["AdminLoginProtection:MaxFailedAttempts"] = "3",
                ["AdminLoginProtection:FailedAttemptWindowMinutes"] = "5",
                ["AdminLoginProtection:LockoutMinutes"] = "1",
                ["AdminLoginProtection:LoginRequestsPerMinute"] = "20"
            });
        });
    }
}
