namespace Showroom.Web.Models;

public sealed class CustomerRequestConfirmationResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? NotificationChannel { get; init; }
}
