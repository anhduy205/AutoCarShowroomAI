using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public class SqlInventoryManagementService : IInventoryManagementService
{
    private const string BrandListSql = """
        SELECT
            b.Id,
            b.Name,
            COUNT(c.Id) AS CarCount
        FROM Brands b
        LEFT JOIN Cars c ON c.BrandId = b.Id
        GROUP BY b.Id, b.Name
        ORDER BY b.Name;
        """;

    private const string BrandByIdSql = """
        SELECT Id, Name
        FROM Brands
        WHERE Id = @Id;
        """;

    private const string InsertBrandSql = """
        INSERT INTO Brands (Name)
        VALUES (@Name);
        """;

    private const string UpdateBrandSql = """
        UPDATE Brands
        SET Name = @Name
        WHERE Id = @Id;
        """;

    private const string DeleteBrandSql = """
        DELETE FROM Brands
        WHERE Id = @Id;
        """;

    private const string BrandOptionsSql = """
        SELECT Id, Name
        FROM Brands
        ORDER BY Name;
        """;

    private const string CarListSql = """
        SELECT
            c.Id,
            c.Name,
            b.Name,
            c.[Year],
            c.[Type],
            c.Color,
            c.Status,
            c.Price,
            c.StockQuantity,
            c.CreatedAt
        FROM Cars c
        INNER JOIN Brands b ON b.Id = c.BrandId
        ORDER BY c.CreatedAt DESC, c.Name;
        """;

    private const string PublicCarListSql = """
        SELECT
            c.Id,
            c.Name,
            b.Name,
            c.[Year],
            c.[Type],
            c.Color,
            c.Price,
            c.ImageUrls
        FROM Cars c
        INNER JOIN Brands b ON b.Id = c.BrandId
        WHERE c.StockQuantity > 0
          AND c.Status IN (N'InStock', N'Promotion')
          AND (@BrandId IS NULL OR c.BrandId = @BrandId)
          AND (@Type IS NULL OR c.[Type] = @Type)
          AND (@MinPrice IS NULL OR c.Price >= @MinPrice)
          AND (@MaxPrice IS NULL OR c.Price <= @MaxPrice)
          AND (@YearFrom IS NULL OR (c.[Year] IS NOT NULL AND c.[Year] >= @YearFrom))
          AND (@YearTo IS NULL OR (c.[Year] IS NOT NULL AND c.[Year] <= @YearTo))
          AND (@QueryLike IS NULL OR c.Name LIKE @QueryLike OR b.Name LIKE @QueryLike)
        ORDER BY c.CreatedAt DESC, c.Name;
        """;

    private const string ChatCarListSql = """
        SELECT TOP (@Take)
            c.Id,
            c.Name,
            b.Name,
            c.[Year],
            c.[Type],
            c.Color,
            c.Status,
            c.Price,
            c.StockQuantity,
            c.Specifications
        FROM Cars c
        INNER JOIN Brands b ON b.Id = c.BrandId
        WHERE c.StockQuantity > 0
          AND c.Status IN (N'InStock', N'Promotion')
          AND (@BrandId IS NULL OR c.BrandId = @BrandId)
          AND (@Type IS NULL OR c.[Type] = @Type)
          AND (@MinPrice IS NULL OR c.Price >= @MinPrice)
          AND (@MaxPrice IS NULL OR c.Price <= @MaxPrice)
          AND (@YearFrom IS NULL OR (c.[Year] IS NOT NULL AND c.[Year] >= @YearFrom))
          AND (@YearTo IS NULL OR (c.[Year] IS NOT NULL AND c.[Year] <= @YearTo))
          AND (@QueryLike IS NULL OR c.Name LIKE @QueryLike OR b.Name LIKE @QueryLike OR c.[Type] LIKE @QueryLike)
        ORDER BY
            CASE WHEN c.Status = N'Promotion' THEN 0 ELSE 1 END,
            c.Price ASC,
            c.CreatedAt DESC;
        """;

    private const string CarByIdSql = """
        SELECT Id, BrandId, Name, [Year], [Type], Color, [Description], Specifications, ImageUrls, Status, Price, StockQuantity
        FROM Cars
        WHERE Id = @Id;
        """;

    private const string CarDetailsByIdSql = """
        SELECT
            c.Id,
            c.Name,
            b.Name,
            c.[Year],
            c.[Type],
            c.Color,
            c.[Description],
            c.Specifications,
            c.ImageUrls,
            c.Status,
            c.Price,
            c.StockQuantity,
            c.CreatedAt
        FROM Cars c
        INNER JOIN Brands b ON b.Id = c.BrandId
        WHERE c.Id = @Id;
        """;

    private const string InsertCarSql = """
        INSERT INTO Cars (BrandId, Name, [Year], [Type], Color, [Description], Specifications, ImageUrls, Status, Price, StockQuantity)
        OUTPUT INSERTED.Id
        VALUES (@BrandId, @Name, @Year, @Type, @Color, @Description, @Specifications, @ImageUrls, @Status, @Price, @StockQuantity);
        """;

    private const string UpdateCarSql = """
        UPDATE Cars
        SET BrandId = @BrandId,
            Name = @Name,
            [Year] = @Year,
            [Type] = @Type,
            Color = @Color,
            [Description] = @Description,
            Specifications = @Specifications,
            ImageUrls = @ImageUrls,
            Status = @Status,
            Price = @Price,
            StockQuantity = @StockQuantity
        WHERE Id = @Id;
        """;

    private const string UpdateCarImageUrlsSql = """
        UPDATE Cars
        SET ImageUrls = @ImageUrls
        WHERE Id = @Id;
        """;

    private const string DeleteCarSql = """
        DELETE FROM Cars
        WHERE Id = @Id;
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlInventoryManagementService> _logger;

    public SqlInventoryManagementService(
        IConfiguration configuration,
        ILogger<SqlInventoryManagementService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BrandListItemViewModel>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(BrandListSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<BrandListItemViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new BrandListItemViewModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CarCount = reader.GetInt32(2)
                });
            }

            return items;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load brand list.");
            throw CreateFriendlyException("Không thể tải danh sách hãng xe từ SQL Server.", ex);
        }
    }

    public async Task<BrandFormViewModel?> GetBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(BrandByIdSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new BrandFormViewModel
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load brand {BrandId}.", id);
            throw CreateFriendlyException("Không thể tải thông tin hãng xe.", ex);
        }
    }

    public async Task CreateBrandAsync(BrandFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRequiredText(model.Name, "Ten hang xe khong duoc de trong.");

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(InsertBrandSql, connection);
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = model.Name.Trim();
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Tên hãng xe đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not create brand {BrandName}.", model.Name);
            throw CreateFriendlyException("Không thể thêm hãng xe mới.", ex);
        }
    }

    public async Task<bool> UpdateBrandAsync(BrandFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRequiredText(model.Name, "Ten hang xe khong duoc de trong.");

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(UpdateBrandSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = model.Name.Trim();
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Tên hãng xe đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not update brand {BrandId}.", model.Id);
            throw CreateFriendlyException("Không thể cập nhật hãng xe.", ex);
        }
    }

    public async Task<bool> DeleteBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(DeleteBrandSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsReferenceConstraintViolation(ex))
        {
            throw CreateFriendlyException("Không thể xoá hãng xe đang có xe thuộc hãng này.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not delete brand {BrandId}.", id);
            throw CreateFriendlyException("Không thể xoá hãng xe.", ex);
        }
    }

    public async Task<IReadOnlyList<CarListItemViewModel>> GetCarsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(CarListSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<CarListItemViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new CarListItemViewModel
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    BrandName = reader.GetString(2),
                    Year = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Type = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Color = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Status = reader.IsDBNull(6) ? CarStatusCatalog.InStock : reader.GetString(6),
                    Price = reader.GetDecimal(7),
                    StockQuantity = reader.GetInt32(8),
                    CreatedAt = reader.GetDateTime(9)
                });
            }

            return items;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load car list.");
            throw CreateFriendlyException("Không thể tải danh sách xe từ SQL Server.", ex);
        }
    }

    public async Task<CarFormViewModel> GetNewCarAsync(CancellationToken cancellationToken = default)
    {
        var model = new CarFormViewModel
        {
            Status = CarStatusCatalog.InStock,
            StatusOptions = CarStatusCatalog.GetSelectList(CarStatusCatalog.InStock)
        };
        await PopulateBrandOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task<CarFormViewModel?> GetCarAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(CarByIdSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var model = new CarFormViewModel
            {
                Id = reader.GetInt32(0),
                BrandId = reader.GetInt32(1),
                Name = reader.GetString(2),
                Year = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Type = reader.IsDBNull(4) ? null : reader.GetString(4),
                Color = reader.IsDBNull(5) ? null : reader.GetString(5),
                Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                Specifications = reader.IsDBNull(7) ? null : reader.GetString(7),
                ImageUrls = reader.IsDBNull(8) ? null : reader.GetString(8),
                Status = reader.IsDBNull(9) ? CarStatusCatalog.InStock : reader.GetString(9),
                Price = reader.GetDecimal(10),
                StockQuantity = reader.GetInt32(11)
            };

            await reader.CloseAsync();
            model.BrandOptions = await ReadBrandOptionsAsync(connection, cancellationToken);
            model.StatusOptions = CarStatusCatalog.GetSelectList(model.Status);
            return model;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load car {CarId}.", id);
            throw CreateFriendlyException("Không thể tải thông tin xe.", ex);
        }
    }

    public async Task<CarDetailsViewModel?> GetCarDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(CarDetailsByIdSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new CarDetailsViewModel
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                BrandName = reader.GetString(2),
                Year = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Type = reader.IsDBNull(4) ? null : reader.GetString(4),
                Color = reader.IsDBNull(5) ? null : reader.GetString(5),
                Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                Specifications = reader.IsDBNull(7) ? null : reader.GetString(7),
                ImageUrls = SplitImageUrls(reader.IsDBNull(8) ? null : reader.GetString(8)),
                Status = reader.IsDBNull(9) ? CarStatusCatalog.InStock : reader.GetString(9),
                Price = reader.GetDecimal(10),
                StockQuantity = reader.GetInt32(11),
                CreatedAt = reader.GetDateTime(12)
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load car details {CarId}.", id);
            throw CreateFriendlyException("KhÃ´ng thá»ƒ táº£i thÃ´ng tin xe.", ex);
        }
    }

    public async Task<IReadOnlyList<PublicCarListItemViewModel>> GetPublicCarsAsync(
        PublicCarSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(PublicCarListSql, connection);

            command.Parameters.Add("@BrandId", SqlDbType.Int).Value = request.BrandId is null ? DBNull.Value : request.BrandId.Value;
            command.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = string.IsNullOrWhiteSpace(request.Type) ? DBNull.Value : request.Type.Trim();

            var minPriceParameter = command.Parameters.Add("@MinPrice", SqlDbType.Decimal);
            minPriceParameter.Precision = 18;
            minPriceParameter.Scale = 2;
            minPriceParameter.Value = request.MinPrice is null ? DBNull.Value : request.MinPrice.Value;

            var maxPriceParameter = command.Parameters.Add("@MaxPrice", SqlDbType.Decimal);
            maxPriceParameter.Precision = 18;
            maxPriceParameter.Scale = 2;
            maxPriceParameter.Value = request.MaxPrice is null ? DBNull.Value : request.MaxPrice.Value;

            command.Parameters.Add("@YearFrom", SqlDbType.Int).Value = request.YearFrom is null ? DBNull.Value : request.YearFrom.Value;
            command.Parameters.Add("@YearTo", SqlDbType.Int).Value = request.YearTo is null ? DBNull.Value : request.YearTo.Value;

            var query = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim();
            command.Parameters.Add("@QueryLike", SqlDbType.NVarChar, 200).Value = query is null ? DBNull.Value : $"%{query}%";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<PublicCarListItemViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var carId = reader.GetInt32(0);
                var rawImages = reader.IsDBNull(7) ? null : reader.GetString(7);
                var firstImage = SplitImageUrls(rawImages).FirstOrDefault();

                items.Add(new PublicCarListItemViewModel
                {
                    Id = carId,
                    Name = reader.GetString(1),
                    BrandName = reader.GetString(2),
                    Year = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Type = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Color = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Price = reader.GetDecimal(6),
                    ThumbnailUrl = ResolveThumbnailUrl(carId, firstImage),
                    Status = CarStatusCatalog.InStock
                });
            }

            return items;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load public car list.");
            throw CreateFriendlyException("Khong the tai danh sach xe tu SQL Server.", ex);
        }
    }

    public async Task<IReadOnlyList<SelectListItem>> GetBrandOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            return await ReadBrandOptionsAsync(connection, cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load brand options.");
            throw CreateFriendlyException("Khong the tai danh sach hang xe.", ex);
        }
    }

    public async Task<IReadOnlyList<CarChatCatalogItem>> GetCarsForChatAsync(
        CarChatSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(ChatCarListSql, connection);

            command.Parameters.Add("@Take", SqlDbType.Int).Value = Math.Clamp(request.Take, 1, 20);
            command.Parameters.Add("@BrandId", SqlDbType.Int).Value = request.BrandId is null ? DBNull.Value : request.BrandId.Value;
            command.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = string.IsNullOrWhiteSpace(request.Type) ? DBNull.Value : request.Type.Trim();

            var minPriceParameter = command.Parameters.Add("@MinPrice", SqlDbType.Decimal);
            minPriceParameter.Precision = 18;
            minPriceParameter.Scale = 2;
            minPriceParameter.Value = request.MinPrice is null ? DBNull.Value : request.MinPrice.Value;

            var maxPriceParameter = command.Parameters.Add("@MaxPrice", SqlDbType.Decimal);
            maxPriceParameter.Precision = 18;
            maxPriceParameter.Scale = 2;
            maxPriceParameter.Value = request.MaxPrice is null ? DBNull.Value : request.MaxPrice.Value;

            command.Parameters.Add("@YearFrom", SqlDbType.Int).Value = request.YearFrom is null ? DBNull.Value : request.YearFrom.Value;
            command.Parameters.Add("@YearTo", SqlDbType.Int).Value = request.YearTo is null ? DBNull.Value : request.YearTo.Value;

            var query = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim();
            command.Parameters.Add("@QueryLike", SqlDbType.NVarChar, 200).Value = query is null ? DBNull.Value : $"%{query}%";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<CarChatCatalogItem>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new CarChatCatalogItem
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    BrandName = reader.GetString(2),
                    Year = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Type = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Color = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Status = reader.IsDBNull(6) ? CarStatusCatalog.InStock : reader.GetString(6),
                    Price = reader.GetDecimal(7),
                    StockQuantity = reader.GetInt32(8),
                    Specifications = reader.IsDBNull(9) ? null : reader.GetString(9)
                });
            }

            return items;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load chat car catalog.");
            throw CreateFriendlyException("Khong the tai danh sach xe tu SQL Server.", ex);
        }
    }

    public async Task PopulateBrandOptionsAsync(CarFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            model.BrandOptions = await ReadBrandOptionsAsync(connection, cancellationToken);
            model.StatusOptions = CarStatusCatalog.GetSelectList(model.Status);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load brand options for car form.");
            throw CreateFriendlyException("Không thể tải danh sách hãng xe.", ex);
        }
    }

    public async Task<int> CreateCarAsync(CarFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRequiredText(model.Name, "Ten xe khong duoc de trong.");

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(InsertCarSql, connection);
            FillCarParameters(command, model, includeId: false);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        catch (SqlException ex) when (IsReferenceConstraintViolation(ex))
        {
            throw CreateFriendlyException("Hãng xe được chọn không hợp lệ.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not create car {CarName}.", model.Name);
            throw CreateFriendlyException("Không thể thêm xe mới.", ex);
        }
    }

    public async Task<bool> UpdateCarAsync(CarFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRequiredText(model.Name, "Ten xe khong duoc de trong.");

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(UpdateCarSql, connection);
            FillCarParameters(command, model, includeId: true);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsReferenceConstraintViolation(ex))
        {
            throw CreateFriendlyException("Hãng xe được chọn không hợp lệ.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not update car {CarId}.", model.Id);
            throw CreateFriendlyException("Không thể cập nhật xe.", ex);
        }
    }

    public async Task<bool> DeleteCarAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(DeleteCarSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsReferenceConstraintViolation(ex))
        {
            throw CreateFriendlyException("Không thể xoá xe đã phát sinh giao dịch.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not delete car {CarId}.", id);
            throw CreateFriendlyException("Không thể xoá xe.", ex);
        }
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("ShowroomDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw CreateFriendlyException("Chưa cấu hình connection string ShowroomDb.");
        }

        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await connection.DisposeAsync();
            throw CreateFriendlyException("Không thể kết nối tới SQL Server.", ex);
        }
    }

    private static async Task<IReadOnlyList<SelectListItem>> ReadBrandOptionsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(BrandOptionsSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<SelectListItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SelectListItem
            {
                Value = reader.GetInt32(0).ToString(),
                Text = reader.GetString(1)
            });
        }

        return items;
    }

    private static void FillCarParameters(SqlCommand command, CarFormViewModel model, bool includeId)
    {
        if (includeId)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
        }

        command.Parameters.Add("@BrandId", SqlDbType.Int).Value = model.BrandId;
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = model.Name.Trim();

        command.Parameters.Add("@Year", SqlDbType.Int).Value = model.Year is null ? DBNull.Value : model.Year.Value;
        command.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = string.IsNullOrWhiteSpace(model.Type) ? DBNull.Value : model.Type.Trim();
        command.Parameters.Add("@Color", SqlDbType.NVarChar, 50).Value = string.IsNullOrWhiteSpace(model.Color) ? DBNull.Value : model.Color.Trim();
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 1000).Value = string.IsNullOrWhiteSpace(model.Description) ? DBNull.Value : model.Description.Trim();
        command.Parameters.Add("@Specifications", SqlDbType.NVarChar, -1).Value = string.IsNullOrWhiteSpace(model.Specifications) ? DBNull.Value : model.Specifications.Trim();
        command.Parameters.Add("@ImageUrls", SqlDbType.NVarChar, -1).Value = string.IsNullOrWhiteSpace(model.ImageUrls) ? DBNull.Value : model.ImageUrls.Trim();
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = string.IsNullOrWhiteSpace(model.Status) ? CarStatusCatalog.InStock : model.Status.Trim();

        var priceParameter = command.Parameters.Add("@Price", SqlDbType.Decimal);
        priceParameter.Precision = 18;
        priceParameter.Scale = 2;
        priceParameter.Value = model.Price;

        command.Parameters.Add("@StockQuantity", SqlDbType.Int).Value = model.StockQuantity;
    }

    public async Task<bool> UpdateCarImageUrlsAsync(int carId, string? imageUrls, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(UpdateCarImageUrlsSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = carId;
            command.Parameters.Add("@ImageUrls", SqlDbType.NVarChar, -1).Value =
                string.IsNullOrWhiteSpace(imageUrls) ? DBNull.Value : imageUrls.Trim();

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not update car images {CarId}.", carId);
            throw CreateFriendlyException("KhÃ´ng thá»ƒ cáº­p nháº­t anh xe.", ex);
        }
    }

    private static IReadOnlyList<string> SplitImageUrls(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToArray();
    }

    private static string? ResolveThumbnailUrl(int carId, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (imageUrl.StartsWith($"/uploads/cars/{carId}/", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = Path.GetFileNameWithoutExtension(imageUrl);
            return $"/uploads/cars/{carId}/thumbs/{baseName}.jpg";
        }

        return imageUrl;
    }

    private static bool IsDuplicateKey(SqlException ex) => ex.Number is 2601 or 2627;

    private static bool IsReferenceConstraintViolation(SqlException ex) => ex.Number == 547;

    private static FriendlyOperationException CreateFriendlyException(string message, Exception? innerException = null)
    {
        if (innerException is SqlException { Number: 207 })
        {
            message =
                "Database schema chua cap nhat (thieu cot/bang). Hay chay `database/upgrade.sql` (hoac `database/setup.sql`) de cap nhat lai.";
        }

        return new FriendlyOperationException(message, innerException);
    }

    private static void ValidateRequiredText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw CreateFriendlyException(message);
        }
    }
}
