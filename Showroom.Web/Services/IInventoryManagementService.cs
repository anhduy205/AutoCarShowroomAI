using Microsoft.AspNetCore.Mvc.Rendering;
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

    Task<CarDetailsViewModel?> GetCarDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicCarListItemViewModel>> GetPublicCarsAsync(PublicCarSearchRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SelectListItem>> GetBrandOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarChatCatalogItem>> GetCarsForChatAsync(CarChatSearchRequest request, CancellationToken cancellationToken = default);

    Task PopulateBrandOptionsAsync(CarFormViewModel model, CancellationToken cancellationToken = default);

    Task<int> CreateCarAsync(CarFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateCarAsync(CarFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateCarImageUrlsAsync(int carId, string? imageUrls, CancellationToken cancellationToken = default);

    Task<bool> DeleteCarAsync(int id, CancellationToken cancellationToken = default);
}
