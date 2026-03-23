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
            c.Price,
            c.StockQuantity,
            c.CreatedAt
        FROM Cars c
        INNER JOIN Brands b ON b.Id = c.BrandId
        ORDER BY c.CreatedAt DESC, c.Name;
        """;

    private const string CarByIdSql = """
        SELECT Id, BrandId, Name, Price, StockQuantity
        FROM Cars
        WHERE Id = @Id;
        """;

    private const string InsertCarSql = """
        INSERT INTO Cars (BrandId, Name, Price, StockQuantity)
        VALUES (@BrandId, @Name, @Price, @StockQuantity);
        """;

    private const string UpdateCarSql = """
        UPDATE Cars
        SET BrandId = @BrandId,
            Name = @Name,
            Price = @Price,
            StockQuantity = @StockQuantity
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
                    Price = reader.GetDecimal(3),
                    StockQuantity = reader.GetInt32(4),
                    CreatedAt = reader.GetDateTime(5)
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
        var model = new CarFormViewModel();
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
                Price = reader.GetDecimal(3),
                StockQuantity = reader.GetInt32(4)
            };

            await reader.CloseAsync();
            model.BrandOptions = await ReadBrandOptionsAsync(connection, cancellationToken);
            return model;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load car {CarId}.", id);
            throw CreateFriendlyException("Không thể tải thông tin xe.", ex);
        }
    }

    public async Task PopulateBrandOptionsAsync(CarFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            model.BrandOptions = await ReadBrandOptionsAsync(connection, cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load brand options for car form.");
            throw CreateFriendlyException("Không thể tải danh sách hãng xe.", ex);
        }
    }

    public async Task CreateCarAsync(CarFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(InsertCarSql, connection);
            FillCarParameters(command, model, includeId: false);
            await command.ExecuteNonQueryAsync(cancellationToken);
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

        var priceParameter = command.Parameters.Add("@Price", SqlDbType.Decimal);
        priceParameter.Precision = 18;
        priceParameter.Scale = 2;
        priceParameter.Value = model.Price;

        command.Parameters.Add("@StockQuantity", SqlDbType.Int).Value = model.StockQuantity;
    }

    private static bool IsDuplicateKey(SqlException ex) => ex.Number is 2601 or 2627;

    private static bool IsReferenceConstraintViolation(SqlException ex) => ex.Number == 547;

    private static InvalidOperationException CreateFriendlyException(string message, Exception? innerException = null)
        => new(message, innerException);
}
