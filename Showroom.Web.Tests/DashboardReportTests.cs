using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Showroom.Web.Services;
using Showroom.Web.Tests.Infrastructure;

namespace Showroom.Web.Tests;

public class DashboardReportTests : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;
    private SqlShowroomDataService? _service;

    public async Task InitializeAsync()
    {
        _database = await SqlServerTestDatabase.CreateAsync();
        _service = CreateService(_database.ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    [Fact]
    public async Task DashboardUsesStockQuantityTotals()
    {
        var dashboard = await _service!.GetDashboardAsync();

        Assert.True(dashboard.IsDatabaseConnected);
        Assert.Equal(21, dashboard.TotalCarsInStock);

        var inventory = dashboard.BrandInventory.ToDictionary(item => item.BrandName, item => item.StockQuantity);
        Assert.Equal(6, inventory["Toyota"]);
        Assert.Equal(9, inventory["Hyundai"]);
        Assert.Equal(1, inventory["Ford"]);
        Assert.Equal(5, inventory["Mazda"]);
    }

    [Fact]
    public async Task BestSellingCarsCanBeFilteredByDateRange()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayDashboard = await _service!.GetDashboardAsync(today, today);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var futureDashboard = await _service.GetDashboardAsync(future, future);

        Assert.NotEmpty(todayDashboard.BestSellingCars);
        Assert.Equal("Hyundai Accent", todayDashboard.BestSellingCars[0].CarName);
        Assert.Equal(2, todayDashboard.BestSellingCars[0].SoldQuantity);
        Assert.Empty(futureDashboard.BestSellingCars);
    }

    private static SqlShowroomDataService CreateService(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ShowroomDb"] = connectionString
            })
            .Build();

        return new SqlShowroomDataService(configuration, NullLogger<SqlShowroomDataService>.Instance);
    }
}
