using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface ISalesProcessService
{
    Task<SalesProcessDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task CreateLeadAsync(LeadFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateOpportunityAsync(SalesOpportunityFormViewModel model, CancellationToken cancellationToken = default);

    Task ScheduleTestDriveAsync(TestDriveFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateQuoteAsync(SalesQuoteFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateContractAsync(SalesContractFormViewModel model, CancellationToken cancellationToken = default);

    Task AddAccessoryAsync(ContractAccessoryFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateRegistrationTaskAsync(RegistrationTaskFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateDeliveryAsync(VehicleDeliveryFormViewModel model, CancellationToken cancellationToken = default);

    Task AddChecklistItemAsync(DeliveryChecklistItemFormViewModel model, CancellationToken cancellationToken = default);
}
