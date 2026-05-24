using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Showroom.Web.Configuration;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class SmtpCustomerNotificationService : ICustomerNotificationService
{
    private readonly ILogger<SmtpCustomerNotificationService> _logger;
    private readonly SmtpOptions _options;

    public SmtpCustomerNotificationService(
        IOptions<SmtpOptions> options,
        ILogger<SmtpCustomerNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CustomerNotificationResult> NotifyConfirmedAsync(
        CustomerRequestDetailsViewModel request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new FriendlyOperationException("Khach hang chua co email. He thong hien chi gui email that, SMS can cau hinh nha cung cap rieng.");
        }

        ValidateOptions();

        var preferredTime = request.PreferredTime?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "showroom se lien he de chot lich";
        var carText = string.IsNullOrWhiteSpace(request.CarName) ? string.Empty : $" cho xe {request.CarName}";
        var nextSteps = CustomerRequestCatalog.GetEmailNextSteps(request.RequestType);
        var subject = $"Showroom da xac nhan yeu cau {request.RequestTypeLabel}";
        var message =
            $"Xin chao {request.CustomerName},\n\n" +
            $"Auto Car Showroom da xac nhan yeu cau {request.RequestTypeLabel.ToLowerInvariant()}{carText} cua ban.\n" +
            $"Thoi gian: {preferredTime}.\n\n" +
            $"{nextSteps}\n\n" +
            "Tran trong,\nAuto Car Showroom";

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromEmail.Trim(), _options.FromName.Trim()),
            Subject = subject,
            Body = message,
            IsBodyHtml = false
        };
        mailMessage.To.Add(new MailAddress(request.CustomerEmail.Trim(), request.CustomerName.Trim()));

        using var smtpClient = new SmtpClient(_options.Host.Trim(), _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username.Trim(), _options.Password)
        };

        try
        {
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation(
                "Sent customer confirmation email to {Email} for request {RequestId}.",
                request.CustomerEmail,
                request.Id);

            return new CustomerNotificationResult("Email", message);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not send customer confirmation email for request {RequestId}.", request.Id);
            throw new FriendlyOperationException("Khong the gui email xac nhan. Vui long kiem tra cau hinh SMTP.", ex);
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host) ||
            _options.Port <= 0 ||
            string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new FriendlyOperationException("Chua cau hinh SMTP nen chua the gui email that.");
        }
    }
}
