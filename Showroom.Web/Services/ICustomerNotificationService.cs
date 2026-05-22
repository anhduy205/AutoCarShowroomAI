using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface ICustomerNotificationService
{
    Task<CustomerNotificationResult> NotifyConfirmedAsync(
        CustomerRequestDetailsViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerNotificationResult(string Channel, string Message);
