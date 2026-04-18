namespace Showroom.Web.Models;

public class AuditLogEntry
{
    public string Username { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public int? EntityId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string IpAddress { get; init; } = string.Empty;
}
