using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Showroom.Web.Services;

public sealed class CarImageStorageService : ICarImageStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CarImageStorageService> _logger;

    public CarImageStorageService(IWebHostEnvironment environment, ILogger<CarImageStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<CarImageSaveResult> SaveAsync(int carId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (carId <= 0)
        {
            throw new FriendlyOperationException("Ma xe khong hop le.");
        }

        if (file is null || file.Length == 0)
        {
            throw new FriendlyOperationException("Vui long chon anh can tai len.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            throw new FriendlyOperationException("Anh qua lon (toi da 5MB).");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new FriendlyOperationException("Dinh dang anh khong duoc ho tro. Chi chap nhan JPG/PNG/WebP.");
        }

        var extension = GetSafeExtension(file.ContentType);
        var fileName = $"{Guid.NewGuid():N}{extension}";

        var relativeFolder = Path.Combine("uploads", "cars", carId.ToString());
        var absoluteFolder = Path.Combine(_environment.WebRootPath, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(absoluteFolder, fileName);
        await using (var stream = File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        string? thumbnailUrl = null;
        try
        {
            thumbnailUrl = await CreateThumbnailAsync(
                carId,
                absolutePath,
                Path.GetFileNameWithoutExtension(fileName),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not generate thumbnail for car {CarId}.", carId);
        }

        var imageUrl = $"/uploads/cars/{carId}/{fileName}";
        return new CarImageSaveResult(imageUrl, thumbnailUrl);
    }

    public Task DeleteAsync(int carId, string imageUrl, CancellationToken cancellationToken = default)
    {
        if (carId <= 0)
        {
            throw new FriendlyOperationException("Ma xe khong hop le.");
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new FriendlyOperationException("Duong dan anh khong hop le.");
        }

        if (!TryMapToPhysicalPaths(carId, imageUrl, out var originalPath, out var thumbnailPath))
        {
            throw new FriendlyOperationException("Chi co the xoa anh duoc tai len tu he thong.");
        }

        TryDeleteFile(originalPath);
        if (!string.IsNullOrWhiteSpace(thumbnailPath))
        {
            TryDeleteFile(thumbnailPath);
        }

        return Task.CompletedTask;
    }

    private static string GetSafeExtension(string contentType)
        => contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

    private async Task<string?> CreateThumbnailAsync(
        int carId,
        string originalAbsolutePath,
        string baseName,
        CancellationToken cancellationToken)
    {
        var thumbsRelativeFolder = Path.Combine("uploads", "cars", carId.ToString(), "thumbs");
        var thumbsAbsoluteFolder = Path.Combine(_environment.WebRootPath, thumbsRelativeFolder);
        Directory.CreateDirectory(thumbsAbsoluteFolder);

        var thumbFileName = $"{baseName}.jpg";
        var thumbAbsolutePath = Path.Combine(thumbsAbsoluteFolder, thumbFileName);

        await using var input = File.OpenRead(originalAbsolutePath);
        using var image = await Image.LoadAsync(input, cancellationToken);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(360, 360)
        }));

        await image.SaveAsJpegAsync(thumbAbsolutePath, cancellationToken);

        return $"/uploads/cars/{carId}/thumbs/{thumbFileName}";
    }

    private bool TryMapToPhysicalPaths(int carId, string imageUrl, out string originalPath, out string? thumbnailPath)
    {
        originalPath = string.Empty;
        thumbnailPath = null;

        var normalized = imageUrl.Trim();
        if (!normalized.StartsWith($"/uploads/cars/{carId}/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return false;
        }

        var originalFolder = Path.Combine(_environment.WebRootPath, "uploads", "cars", carId.ToString());
        originalPath = Path.Combine(originalFolder, fileName);

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var thumbsFolder = Path.Combine(originalFolder, "thumbs");
        thumbnailPath = Path.Combine(thumbsFolder, $"{baseName}.jpg");
        return true;
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete file {Path}.", path);
        }
    }
}

