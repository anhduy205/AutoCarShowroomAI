using System.Net;
using System.Text.RegularExpressions;
using Showroom.Web.Tests.Infrastructure;

namespace Showroom.Web.Tests;

public class AuthIntegrationTests
{
    [Fact]
    public async Task AnonymousUserIsRedirectedToLogin()
    {
        await using var factory = new ShowroomWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var response = await client.GetAsync("/Orders");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith("/Admin/Login?ReturnUrl=%2FOrders", response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginIsLockedAfterRepeatedFailures()
    {
        await using var factory = new ShowroomWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await PostLoginAsync(client, "admin", "WrongPassword!");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var lockedResponse = await PostLoginAsync(client, "admin", "WrongPassword!");
        var lockedContent = await lockedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, lockedResponse.StatusCode);
        Assert.Contains("Tai khoan tam khoa", lockedContent);

        var blockedValidResponse = await PostLoginAsync(client, "admin", ShowroomWebApplicationFactory.DefaultPassword);
        var blockedValidContent = await blockedValidResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, blockedValidResponse.StatusCode);
        Assert.Contains("Tai khoan tam khoa", blockedValidContent);
    }

    private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string username, string password)
    {
        var antiForgeryToken = await GetAntiForgeryTokenAsync(client);
        var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = username,
            ["Password"] = password,
            ["ReturnUrl"] = string.Empty,
            ["__RequestVerificationToken"] = antiForgeryToken
        });

        return await client.PostAsync("/Admin/Login", payload);
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/Admin/Login");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);

        Assert.True(match.Success, "Antiforgery token was not found in the login page.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
