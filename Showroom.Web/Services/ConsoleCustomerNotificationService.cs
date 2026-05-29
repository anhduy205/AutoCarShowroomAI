using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class ConsoleCustomerNotificationService : ICustomerNotificationService
{
    private readonly ILogger<ConsoleCustomerNotificationService> _logger;

    public ConsoleCustomerNotificationService(ILogger<ConsoleCustomerNotificationService> logger)
    {
        _logger = logger;
    }

    public Task<CustomerNotificationResult> NotifyConfirmedAsync(
        CustomerRequestDetailsViewModel request,
        CancellationToken cancellationToken = default)
    {
        var channel = !string.IsNullOrWhiteSpace(request.CustomerEmail) ? "Email" : "SMS";
        var destination = channel == "Email" ? request.CustomerEmail : request.CustomerPhone;
        var preferredTime = request.PreferredTime?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "theo lịch showroom sẽ liên hệ";
        var carText = string.IsNullOrWhiteSpace(request.CarName) ? string.Empty : $" cho xe {request.CarName}";
        var message =
            $"Showroom đã xác nhận yêu cầu {request.RequestTypeLabel.ToLowerInvariant()}{carText} của bạn. Thoi gian: {preferredTime}.";

        _logger.LogInformation(
            "Customer notification via {Channel} to {Destination}: {Message}",
            channel,
            destination,
            message);

        return Task.FromResult(new CustomerNotificationResult(channel, message));
    }
}
