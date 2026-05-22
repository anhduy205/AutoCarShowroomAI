namespace Showroom.Web.Models;

public partial class CustomerRequest
{
    public int Id { get; set; }

    public string RequestType { get; set; } = null!;

    public int? CarId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerPhone { get; set; }

    public string? CustomerEmail { get; set; }

    public DateTime? PreferredTime { get; set; }

    public decimal? DepositAmount { get; set; }

    public string? Note { get; set; }

    public string Status { get; set; } = null!;

    public string? NotificationChannel { get; set; }

    public DateTime? NotificationSentAt { get; set; }

    public string? NotificationMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public virtual Car? Car { get; set; }
}
