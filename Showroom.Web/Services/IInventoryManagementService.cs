using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface IInventoryManagementService
{
    Task<IReadOnlyList<BrandListItemViewModel>> GetBrandsAsync(CancellationToken cancellationToken = default);

    Task<BrandFormViewModel?> GetBrandAsync(int id, CancellationToken cancellationToken = default);

    Task CreateBrandAsync(BrandFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateBrandAsync(BrandFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteBrandAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarListItemViewModel>> GetCarsAsync(CancellationToken cancellationToken = default);

    Task<CarFormViewModel> GetNewCarAsync(CancellationToken cancellationToken = default);

    Task<CarFormViewModel?> GetCarAsync(int id, CancellationToken cancellationToken = default);

    Task PopulateBrandOptionsAsync(CarFormViewModel model, CancellationToken cancellationToken = default);

    Task CreateCarAsync(CarFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateCarAsync(CarFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteCarAsync(int id, CancellationToken cancellationToken = default);
}
