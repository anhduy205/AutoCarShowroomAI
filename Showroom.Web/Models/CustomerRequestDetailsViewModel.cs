namespace Showroom.Web.Models;

public sealed class CustomerRequestDetailsViewModel
{
    public int Id { get; set; }

    public string RequestType { get; set; } = string.Empty;

    public string RequestTypeLabel => CustomerRequestCatalog.GetTypeLabel(RequestType);

    public int? CarId { get; set; }

    public string? CarName { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string? CustomerEmail { get; set; }

    public DateTime? PreferredTime { get; set; }

    public decimal? DepositAmount { get; set; }

    public string? Note { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusLabel => CustomerRequestCatalog.GetStatusLabel(Status);

    public string? NotificationChannel { get; set; }

    public DateTime? NotificationSentAt { get; set; }

    public string? NotificationMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }
}
