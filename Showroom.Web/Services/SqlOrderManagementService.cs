using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public class SqlOrderManagementService : IOrderManagementService
{
    private const string OrderListSql = """
        SELECT
            o.Id,
            o.CustomerName,
            o.Status,
            ISNULL(SUM(oi.Quantity), 0) AS TotalQuantity,
            ISNULL(SUM(oi.Quantity * oi.UnitPrice), 0) AS TotalAmount,
            o.CreatedAt
        FROM Orders o
        LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
        GROUP BY o.Id, o.CustomerName, o.Status, o.CreatedAt
        ORDER BY o.CreatedAt DESC, o.Id DESC;
        """;

    private const string OrderHeaderByIdSql = """
        SELECT CustomerName, Status
        FROM Orders
        WHERE Id = @Id;
        """;

    private const string OrderItemsByOrderIdSql = """
        SELECT CarId, Quantity
        FROM OrderItems
        WHERE OrderId = @OrderId
        ORDER BY Id;
        """;

    private const string CarOptionsSql = """
        SELECT
            c.Id,
            c.Name,
            b.Name,
            c.StockQuantity
        FROM Cars c
        INNER JOIN Brands b ON b.Id = c.BrandId
        ORDER BY b.Name, c.Name;
        """;

    private const string CarSnapshotSql = """
        SELECT
            c.Id,
            c.Name,
            c.Price
        FROM Cars c
        WHERE c.Id = @Id;
        """;

    private const string InsertOrderSql = """
        INSERT INTO Orders (CustomerName, Status)
        OUTPUT INSERTED.Id
        VALUES (@CustomerName, @Status);
        """;

    private const string UpdateOrderSql = """
        UPDATE Orders
        SET CustomerName = @CustomerName,
            Status = @Status
        WHERE Id = @Id;
        """;

    private const string DeleteOrderItemsSql = """
        DELETE FROM OrderItems
        WHERE OrderId = @OrderId;
        """;

    private const string DeleteOrderSql = """
        DELETE FROM Orders
        WHERE Id = @Id;
        """;

    private const string InsertOrderItemSql = """
        INSERT INTO OrderItems (OrderId, CarId, Quantity, UnitPrice)
        VALUES (@OrderId, @CarId, @Quantity, @UnitPrice);
        """;

    private const string RestoreStockSql = """
        UPDATE Cars
        SET StockQuantity = StockQuantity + @Quantity
        WHERE Id = @CarId;
        """;

    private const string ReduceStockSql = """
        UPDATE Cars
        SET StockQuantity = StockQuantity - @Quantity
        WHERE Id = @CarId
          AND StockQuantity >= @Quantity;
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlOrderManagementService> _logger;

    public SqlOrderManagementService(
        IConfiguration configuration,
        ILogger<SqlOrderManagementService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OrderListItemViewModel>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(OrderListSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<OrderListItemViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new OrderListItemViewModel
                {
                    Id = reader.GetInt32(0),
                    CustomerName = reader.GetString(1),
                    Status = reader.GetString(2),
                    TotalQuantity = reader.GetInt32(3),
                    TotalAmount = reader.GetDecimal(4),
                    CreatedAt = reader.GetDateTime(5)
                });
            }

            return items;
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load order list.");
            throw CreateFriendlyException("Khong the tai danh sach don hang tu SQL Server.", ex);
        }
    }

    public async Task<OrderFormViewModel> GetNewOrderAsync(CancellationToken cancellationToken = default)
    {
        var model = new OrderFormViewModel();
        await PopulateCarOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task<OrderFormViewModel?> GetOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);

            var header = await ReadOrderHeaderAsync(id, connection, transaction: null, cancellationToken);
            if (header is null)
            {
                return null;
            }

            var model = new OrderFormViewModel
            {
                Id = id,
                CustomerName = header.CustomerName,
                Status = header.Status,
                Items = await ReadOrderItemsAsync(id, connection, transaction: null, cancellationToken)
            };

            model.CarOptions = await ReadCarOptionsAsync(connection, transaction: null, cancellationToken);
            return model;
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load order {OrderId}.", id);
            throw CreateFriendlyException("Khong the tai thong tin don hang.", ex);
        }
    }

    public async Task PopulateCarOptionsAsync(OrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            model.CarOptions = await ReadCarOptionsAsync(connection, transaction: null, cancellationToken);
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load car options for order form.");
            throw CreateFriendlyException("Khong the tai danh sach xe cho don hang.", ex);
        }
    }

    public async Task<int> CreateOrderAsync(OrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var carSnapshots = await LoadCarSnapshotsAsync(model.Items, connection, transaction, cancellationToken);
                var orderId = await InsertOrderAsync(model, connection, transaction, cancellationToken);

                foreach (var item in model.Items)
                {
                    await InsertOrderItemAsync(orderId, item, carSnapshots[item.CarId].Price, connection, transaction, cancellationToken);
                }

                if (OrderStatusCatalog.CountsTowardSales(model.Status))
                {
                    await ReduceStockAsync(model.Items, carSnapshots, connection, transaction, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return orderId;
            }
            catch
            {
                if (transaction.Connection is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not create order for customer {CustomerName}.", model.CustomerName);
            throw CreateFriendlyException("Khong the tao don hang moi.", ex);
        }
    }

    public async Task<bool> UpdateOrderAsync(OrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingOrder = await ReadOrderHeaderAsync(model.Id, connection, transaction, cancellationToken);
                if (existingOrder is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                var existingItems = await ReadOrderItemsAsync(model.Id, connection, transaction, cancellationToken);
                if (OrderStatusCatalog.CountsTowardSales(existingOrder.Status))
                {
                    await RestoreStockAsync(existingItems, connection, transaction, cancellationToken);
                }

                var affected = await UpdateOrderHeaderAsync(model, connection, transaction, cancellationToken);
                if (affected == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                await DeleteOrderItemsAsync(model.Id, connection, transaction, cancellationToken);

                var carSnapshots = await LoadCarSnapshotsAsync(model.Items, connection, transaction, cancellationToken);
                foreach (var item in model.Items)
                {
                    await InsertOrderItemAsync(model.Id, item, carSnapshots[item.CarId].Price, connection, transaction, cancellationToken);
                }

                if (OrderStatusCatalog.CountsTowardSales(model.Status))
                {
                    await ReduceStockAsync(model.Items, carSnapshots, connection, transaction, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch
            {
                if (transaction.Connection is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not update order {OrderId}.", model.Id);
            throw CreateFriendlyException("Khong the cap nhat don hang.", ex);
        }
    }

    public async Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingOrder = await ReadOrderHeaderAsync(id, connection, transaction, cancellationToken);
                if (existingOrder is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                var existingItems = await ReadOrderItemsAsync(id, connection, transaction, cancellationToken);
                if (OrderStatusCatalog.CountsTowardSales(existingOrder.Status))
                {
                    await RestoreStockAsync(existingItems, connection, transaction, cancellationToken);
                }

                await DeleteOrderItemsAsync(id, connection, transaction, cancellationToken);
                var deleted = await DeleteOrderCoreAsync(id, connection, transaction, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return deleted > 0;
            }
            catch
            {
                if (transaction.Connection is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not delete order {OrderId}.", id);
            throw CreateFriendlyException("Khong the xoa don hang.", ex);
        }
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("ShowroomDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw CreateFriendlyException("Chua cau hinh connection string ShowroomDb.");
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
            throw CreateFriendlyException("Khong the ket noi toi SQL Server.", ex);
        }
    }

    private static async Task<OrderHeaderSnapshot?> ReadOrderHeaderAsync(
        int id,
        SqlConnection connection,
        SqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(OrderHeaderByIdSql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new OrderHeaderSnapshot(reader.GetString(0), reader.GetString(1));
    }

    private static async Task<List<OrderFormItemViewModel>> ReadOrderItemsAsync(
        int orderId,
        SqlConnection connection,
        SqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(OrderItemsByOrderIdSql, connection, transaction);
        command.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<OrderFormItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new OrderFormItemViewModel
            {
                CarId = reader.GetInt32(0),
                Quantity = reader.GetInt32(1)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<SelectListItem>> ReadCarOptionsAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(CarOptionsSql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<SelectListItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SelectListItem
            {
                Value = reader.GetInt32(0).ToString(),
                Text = $"{reader.GetString(2)} - {reader.GetString(1)} (ton kho: {reader.GetInt32(3)})"
            });
        }

        return items;
    }

    private static async Task<Dictionary<int, CarSnapshot>> LoadCarSnapshotsAsync(
        IEnumerable<OrderFormItemViewModel> items,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var snapshots = new Dictionary<int, CarSnapshot>();

        foreach (var carId in items.Select(item => item.CarId).Distinct())
        {
            await using var command = CreateCommand(CarSnapshotSql, connection, transaction);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = carId;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw CreateFriendlyException("Xe duoc chon khong hop le.");
            }

            snapshots[carId] = new CarSnapshot(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetDecimal(2));
        }

        return snapshots;
    }

    private static async Task<int> InsertOrderAsync(
        OrderFormViewModel model,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(InsertOrderSql, connection, transaction);
        command.Parameters.Add("@CustomerName", SqlDbType.NVarChar, 150).Value = model.CustomerName.Trim();
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = model.Status;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task<int> UpdateOrderHeaderAsync(
        OrderFormViewModel model,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(UpdateOrderSql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
        command.Parameters.Add("@CustomerName", SqlDbType.NVarChar, 150).Value = model.CustomerName.Trim();
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = model.Status;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteOrderItemsAsync(
        int orderId,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(DeleteOrderItemsSql, connection, transaction);
        command.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> DeleteOrderCoreAsync(
        int orderId,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(DeleteOrderSql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = orderId;
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertOrderItemAsync(
        int orderId,
        OrderFormItemViewModel item,
        decimal unitPrice,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(InsertOrderItemSql, connection, transaction);
        command.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;
        command.Parameters.Add("@CarId", SqlDbType.Int).Value = item.CarId;
        command.Parameters.Add("@Quantity", SqlDbType.Int).Value = item.Quantity;

        var priceParameter = command.Parameters.Add("@UnitPrice", SqlDbType.Decimal);
        priceParameter.Precision = 18;
        priceParameter.Scale = 2;
        priceParameter.Value = unitPrice;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RestoreStockAsync(
        IEnumerable<OrderFormItemViewModel> items,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            await using var command = CreateCommand(RestoreStockSql, connection, transaction);
            command.Parameters.Add("@CarId", SqlDbType.Int).Value = item.CarId;
            command.Parameters.Add("@Quantity", SqlDbType.Int).Value = item.Quantity;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task ReduceStockAsync(
        IEnumerable<OrderFormItemViewModel> items,
        IReadOnlyDictionary<int, CarSnapshot> carSnapshots,
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            await using var command = CreateCommand(ReduceStockSql, connection, transaction);
            command.Parameters.Add("@CarId", SqlDbType.Int).Value = item.CarId;
            command.Parameters.Add("@Quantity", SqlDbType.Int).Value = item.Quantity;
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (affected == 0)
            {
                throw CreateFriendlyException($"Khong du ton kho cho xe '{carSnapshots[item.CarId].Name}'.");
            }
        }
    }

    private static SqlCommand CreateCommand(string sql, SqlConnection connection, SqlTransaction? transaction)
    {
        var command = new SqlCommand(sql, connection);
        if (transaction is not null)
        {
            command.Transaction = transaction;
        }

        return command;
    }

    private static FriendlyOperationException CreateFriendlyException(string message, Exception? innerException = null)
        => new(message, innerException);

    private sealed record OrderHeaderSnapshot(string CustomerName, string Status);

    private sealed record CarSnapshot(int Id, string Name, decimal Price);
}
