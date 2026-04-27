namespace Showroom.Web.Services;

public interface ICarImageStorageService
{
    Task<CarImageSaveResult> SaveAsync(int carId, IFormFile file, CancellationToken cancellationToken = default);

    Task DeleteAsync(int carId, string imageUrl, CancellationToken cancellationToken = default);
}

public sealed record CarImageSaveResult(string ImageUrl, string? ThumbnailUrl = null);

