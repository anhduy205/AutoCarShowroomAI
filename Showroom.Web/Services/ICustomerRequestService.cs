using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface ICustomerRequestService
{
    Task<CustomerRequestFormViewModel> GetNewRequestAsync(
        int? carId,
        string? requestType,
        CancellationToken cancellationToken = default);

    Task<int> CreateRequestAsync(
        CustomerRequestFormViewModel model,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerRequestListItemViewModel>> GetRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<CustomerRequestDetailsViewModel?> GetRequestAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<CustomerRequestConfirmationResult> ConfirmAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task PopulateOptionsAsync(
        CustomerRequestFormViewModel model,
        CancellationToken cancellationToken = default);
}
