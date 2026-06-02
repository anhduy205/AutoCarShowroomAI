using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface IInventoryProcessService
{
    Task<InventoryProcessDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task CreateVehicleModelAsync(VehicleModelFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateLocationAsync(LocationFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateNewVehicleAsync(NewVehicleFormViewModel model, CancellationToken cancellationToken = default);

    Task MoveVehicleAsync(VehicleMovementFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateFactoryOrderAsync(FactoryOrderFormViewModel model, CancellationToken cancellationToken = default);

    Task CreatePartAsync(PartFormViewModel model, CancellationToken cancellationToken = default);

    Task AdjustPartInventoryAsync(PartInventoryAdjustmentFormViewModel model, CancellationToken cancellationToken = default);

    Task CreatePartPurchaseOrderAsync(PartPurchaseOrderFormViewModel model, CancellationToken cancellationToken = default);

    Task ReceivePartPurchaseOrderItemAsync(PartPurchaseReceiptFormViewModel model, CancellationToken cancellationToken = default);
}
