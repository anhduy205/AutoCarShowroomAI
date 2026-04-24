using System.Data;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public class SqlShowroomDataService : IShowroomDataService
{
    private const string TotalCarsSql = """
        SELECT ISNULL(SUM(StockQuantity), 0)
        FROM Cars;
        """;

    private const string CarsByBrandSql = """
        SELECT
            b.Name,
            ISNULL(SUM(c.StockQuantity), 0) AS StockQuantity
        FROM Brands b
        LEFT JOIN Cars c ON c.BrandId = b.Id
        GROUP BY b.Name
        ORDER BY StockQuantity DESC, b.Name;
        """;

    private const string BestSellingCarsSql = """
        SELECT TOP (@Take)
            c.Name,
            b.Name,
            SUM(oi.Quantity) AS SoldQuantity
        FROM OrderItems oi
        INNER JOIN Cars c ON c.Id = oi.CarId
        INNER JOIN Brands b ON b.Id = c.BrandId
        INNER JOIN Orders o ON o.Id = oi.OrderId
        WHERE o.Status IN ('Completed', 'Paid', 'Delivered')
          AND (@SalesFrom IS NULL OR o.CreatedAt >= @SalesFrom)
          AND (@SalesToExclusive IS NULL OR o.CreatedAt < @SalesToExclusive)
        GROUP BY c.Name, b.Name
        ORDER BY SUM(oi.Quantity) DESC, c.Name;
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlShowroomDataService> _logger;

    public SqlShowroomDataService(IConfiguration configuration, ILogger<SqlShowroomDataService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(
        DateOnly? salesFrom = null,
        DateOnly? salesTo = null,
        CancellationToken cancellationToken = default)
    {
        NormalizeDateRange(ref salesFrom, ref salesTo);

        var connectionString = _configuration.GetConnectionString("ShowroomDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new AdminDashboardViewModel
            {
                SalesFrom = salesFrom,
                SalesTo = salesTo,
                IsDatabaseConnected = false,
                StatusMessage = "Chua cau hinh chuoi ket noi 'ShowroomDb' trong appsettings.json."
            };
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var totalCars = await ReadTotalCarsAsync(connection, cancellationToken);
            var brandInventory = await ReadBrandInventoryAsync(connection, cancellationToken);
            var bestSellingCars = await ReadBestSellingCarsAsync(connection, salesFrom, salesTo, cancellationToken);

            return new AdminDashboardViewModel
            {
                TotalCarsInStock = totalCars,
                BrandInventory = brandInventory,
                BestSellingCars = bestSellingCars,
                SalesFrom = salesFrom,
                SalesTo = salesTo,
                IsDatabaseConnected = true,
                StatusMessage = "Ket noi SQL Server thanh cong."
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load showroom statistics from SQL Server.");

            return new AdminDashboardViewModel
            {
                SalesFrom = salesFrom,
                SalesTo = salesTo,
                IsDatabaseConnected = false,
                StatusMessage = "Khong the tai du lieu showroom. Hay kiem tra cau hinh database va chay database/setup.sql neu schema chua duoc tao."
            };
        }
    }

    private static async Task<int> ReadTotalCarsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(TotalCarsSql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    private static async Task<IReadOnlyList<BrandInventoryItem>> ReadBrandInventoryAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(CarsByBrandSql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<BrandInventoryItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new BrandInventoryItem
            {
                BrandName = reader.GetString(0),
                StockQuantity = reader.GetInt32(1)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<TopSellingCarItem>> ReadBestSellingCarsAsync(
        SqlConnection connection,
        DateOnly? salesFrom,
        DateOnly? salesTo,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(BestSellingCarsSql, connection);
        command.Parameters.AddWithValue("@Take", 5);
        command.Parameters.Add("@SalesFrom", SqlDbType.DateTime2).Value = ToSqlDateTimeOrNull(salesFrom);
        command.Parameters.Add("@SalesToExclusive", SqlDbType.DateTime2).Value = ToSqlDateTimeOrNull(salesTo?.AddDays(1));

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<TopSellingCarItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new TopSellingCarItem
            {
                CarName = reader.GetString(0),
                BrandName = reader.GetString(1),
                SoldQuantity = reader.GetInt32(2)
            });
        }

        return items;
    }

    private static void NormalizeDateRange(ref DateOnly? salesFrom, ref DateOnly? salesTo)
    {
        if (salesFrom is not null && salesTo is not null && salesFrom > salesTo)
        {
            (salesFrom, salesTo) = (salesTo, salesFrom);
        }
    }

    private static object ToSqlDateTimeOrNull(DateOnly? value)
    {
        if (value is null)
        {
            return DBNull.Value;
        }

        return DateTime.SpecifyKind(value.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
    }
}
