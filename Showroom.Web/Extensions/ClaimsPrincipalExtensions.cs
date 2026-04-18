using System.Security.Claims;

namespace Showroom.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetDisplayName(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public static string GetUsername(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public static string GetPrimaryRole(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
