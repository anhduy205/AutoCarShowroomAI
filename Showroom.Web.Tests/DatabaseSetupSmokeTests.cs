using Microsoft.Data.SqlClient;
using Showroom.Web.Tests.Infrastructure;

namespace Showroom.Web.Tests;

public class DatabaseSetupSmokeTests : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public async Task InitializeAsync()
    {
        _database = await SqlServerTestDatabase.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [Fact]
    public async Task SetupScriptSeedsConsistentInventoryAndSalesData()
    {
        await using var connection = new SqlConnection(_database!.ConnectionString);
        await connection.OpenAsync();

        var totalStock = await ExecuteScalarAsync<int>(
            connection,
            "SELECT ISNULL(SUM(StockQuantity), 0) FROM Cars;");
        var totalSold = await ExecuteScalarAsync<int>(
            connection,
            """
            SELECT ISNULL(SUM(oi.Quantity), 0)
            FROM OrderItems oi
            INNER JOIN Orders o ON o.Id = oi.OrderId
            WHERE o.Status IN (N'Paid', N'Completed', N'Delivered');
            """);
        var invalidStatuses = await ExecuteScalarAsync<int>(
            connection,
            """
            SELECT COUNT(*)
            FROM Orders
            WHERE Status NOT IN (N'Pending', N'Paid', N'Completed', N'Delivered', N'Cancelled');
            """);

        Assert.Equal(21, totalStock);
        Assert.Equal(6, totalSold);
        Assert.Equal(0, invalidStatuses);
    }

    private static async Task<T> ExecuteScalarAsync<T>(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull
            ? throw new InvalidOperationException("Expected scalar query to return a non-null value.")
            : (T)Convert.ChangeType(result, typeof(T));
    }
}
