namespace Showroom.Web.Models;

public sealed class CustomerRequestListItemViewModel
{
    public int Id { get; init; }

    public string RequestType { get; init; } = string.Empty;

    public string RequestTypeLabel => CustomerRequestCatalog.GetTypeLabel(RequestType);

    public string CustomerName { get; init; } = string.Empty;

    public string? CustomerPhone { get; init; }

    public string? CustomerEmail { get; init; }

    public string? CarName { get; init; }

    public DateTime? PreferredTime { get; init; }

    public decimal? DepositAmount { get; init; }

    public string Status { get; init; } = string.Empty;

    public string StatusLabel => CustomerRequestCatalog.GetStatusLabel(Status);

    public string? NotificationChannel { get; init; }

    public DateTime? NotificationSentAt { get; init; }

    public DateTime CreatedAt { get; init; }
}
