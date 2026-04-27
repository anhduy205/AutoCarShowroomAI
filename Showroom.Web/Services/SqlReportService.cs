using System.Data;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;
using Showroom.Web.Security;

namespace Showroom.Web.Services;

public sealed class SqlReportService : IReportService
{
    private const string BrandOptionsSql = """
        SELECT Id, Name
        FROM Brands
        ORDER BY Name;
        """;

    private const string SalesByCarSql = """
        SELECT TOP (@Take)
            c.Id,
            c.Name,
            b.Name,
            SUM(oi.Quantity) AS SoldQuantity,
            SUM(oi.Quantity * oi.UnitPrice) AS Amount
        FROM OrderItems oi
        INNER JOIN Orders o ON o.Id = oi.OrderId
        INNER JOIN Cars c ON c.Id = oi.CarId
        INNER JOIN Brands b ON b.Id = c.BrandId
        WHERE o.Status IN (N'Paid', N'Completed', N'Delivered')
          AND (@From IS NULL OR o.CreatedAt >= @From)
          AND (@ToExclusive IS NULL OR o.CreatedAt < @ToExclusive)
          AND (@BrandId IS NULL OR c.BrandId = @BrandId)
        GROUP BY c.Id, c.Name, b.Name
        ORDER BY SUM(oi.Quantity) DESC, SUM(oi.Quantity * oi.UnitPrice) DESC, c.Name;
        """;

    private const string SalesByBrandSql = """
        SELECT
            b.Name,
            SUM(oi.Quantity) AS SoldQuantity,
            SUM(oi.Quantity * oi.UnitPrice) AS Amount
        FROM OrderItems oi
        INNER JOIN Orders o ON o.Id = oi.OrderId
        INNER JOIN Cars c ON c.Id = oi.CarId
        INNER JOIN Brands b ON b.Id = c.BrandId
        WHERE o.Status IN (N'Paid', N'Completed', N'Delivered')
          AND (@From IS NULL OR o.CreatedAt >= @From)
          AND (@ToExclusive IS NULL OR o.CreatedAt < @ToExclusive)
        GROUP BY b.Name
        ORDER BY SUM(oi.Quantity) DESC, b.Name;
        """;

    private const string InventoryListSql = """
        SELECT TOP (@Take)
            c.Id,
            c.Name,
            b.Name,
            c.Status,
            c.Price,
            c.StockQuantity,
            c.[Year],
            c.[Type]
        FROM Cars c
        INNER JOIN Brands b ON b.Id = c.BrandId
        WHERE (@BrandId IS NULL OR c.BrandId = @BrandId)
          AND (@Status IS NULL OR c.Status = @Status)
        ORDER BY
            CASE WHEN c.Status = N'Promotion' THEN 0 WHEN c.Status = N'InStock' THEN 1 ELSE 2 END,
            c.StockQuantity DESC,
            c.CreatedAt DESC;
        """;

    private const string InventoryStatusSummarySql = """
        SELECT
            c.Status,
            COUNT(*) AS CarCount,
            ISNULL(SUM(c.StockQuantity), 0) AS StockQuantity
        FROM Cars c
        WHERE (@BrandId IS NULL OR c.BrandId = @BrandId)
        GROUP BY c.Status
        ORDER BY c.Status;
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlReportService> _logger;

    public SqlReportService(IConfiguration configuration, ILogger<SqlReportService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SelectListItem>> GetBrandOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
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
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load brand options for reports.");
            throw new FriendlyOperationException("Khong the tai danh sach hang xe.", ex);
        }
    }

    public async Task<SalesReportViewModel> GetSalesReportAsync(SalesReportRequest request, CancellationToken cancellationToken = default)
    {
        NormalizeDateRange(ref request);

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);

            var topCars = await ReadSalesByCarAsync(connection, request, cancellationToken);
            var brandSummary = await ReadSalesByBrandAsync(connection, request, cancellationToken);

            var brandLabel = await ResolveBrandLabelAsync(connection, request.BrandId, cancellationToken);

            return new SalesReportViewModel
            {
                From = request.From,
                To = request.To,
                BrandId = request.BrandId,
                BrandLabel = brandLabel,
                TotalQuantity = brandSummary.Sum(x => x.SoldQuantity),
                TotalAmount = brandSummary.Sum(x => x.Amount),
                TopCars = topCars,
                Brands = brandSummary
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load sales report.");
            throw new FriendlyOperationException("Khong the tai bao cao ban hang tu SQL Server.", ex);
        }
    }

    public async Task<InventoryReportViewModel> GetInventoryReportAsync(InventoryReportRequest request, CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim();
        if (status is not null && !CarStatusCatalog.IsValid(status))
        {
            status = null;
        }

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);

            var cars = await ReadInventoryListAsync(connection, request.BrandId, status, request.Take, cancellationToken);
            var summary = await ReadInventoryStatusSummaryAsync(connection, request.BrandId, cancellationToken);
            var brandLabel = await ResolveBrandLabelAsync(connection, request.BrandId, cancellationToken);

            return new InventoryReportViewModel
            {
                BrandId = request.BrandId,
                BrandLabel = brandLabel,
                Status = status,
                TotalCars = cars.Count,
                TotalStockQuantity = cars.Sum(x => x.StockQuantity),
                Cars = cars,
                StatusSummary = summary
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load inventory report.");
            throw new FriendlyOperationException("Khong the tai bao cao ton kho tu SQL Server.", ex);
        }
    }

    private static void NormalizeDateRange(ref SalesReportRequest request)
    {
        var from = request.From;
        var to = request.To;
        if (from is not null && to is not null && from > to)
        {
            (from, to) = (to, from);
        }

        request = new SalesReportRequest
        {
            From = from,
            To = to,
            BrandId = request.BrandId,
            Take = Math.Clamp(request.Take, 1, 500)
        };
    }

    private static async Task<IReadOnlyList<SalesReportCarRow>> ReadSalesByCarAsync(
        SqlConnection connection,
        SalesReportRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(SalesByCarSql, connection);
        command.Parameters.Add("@Take", SqlDbType.Int).Value = request.Take;
        command.Parameters.Add("@From", SqlDbType.DateTime2).Value = ToSqlDateTimeOrNull(request.From);
        command.Parameters.Add("@ToExclusive", SqlDbType.DateTime2).Value = ToSqlDateTimeOrNull(request.To?.AddDays(1));
        command.Parameters.Add("@BrandId", SqlDbType.Int).Value = request.BrandId is null ? DBNull.Value : request.BrandId.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<SalesReportCarRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SalesReportCarRow
            {
                CarId = reader.GetInt32(0),
                CarName = reader.GetString(1),
                BrandName = reader.GetString(2),
                SoldQuantity = reader.GetInt32(3),
                Amount = reader.GetDecimal(4)
            });
        }

        return rows;
    }

    private static async Task<IReadOnlyList<SalesReportBrandRow>> ReadSalesByBrandAsync(
        SqlConnection connection,
        SalesReportRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(SalesByBrandSql, connection);
        command.Parameters.Add("@From", SqlDbType.DateTime2).Value = ToSqlDateTimeOrNull(request.From);
        command.Parameters.Add("@ToExclusive", SqlDbType.DateTime2).Value = ToSqlDateTimeOrNull(request.To?.AddDays(1));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<SalesReportBrandRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SalesReportBrandRow
            {
                BrandName = reader.GetString(0),
                SoldQuantity = reader.GetInt32(1),
                Amount = reader.GetDecimal(2)
            });
        }

        if (request.BrandId is null)
        {
            return rows;
        }

        // If a brand is filtered, keep the brand summary aligned to that filter by trimming to the resolved brand label.
        var brandLabel = await ResolveBrandLabelAsync(connection, request.BrandId, cancellationToken);
        return rows.Where(x => string.Equals(x.BrandName, brandLabel, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static async Task<IReadOnlyList<InventoryReportRow>> ReadInventoryListAsync(
        SqlConnection connection,
        int? brandId,
        string? status,
        int take,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(InventoryListSql, connection);
        command.Parameters.Add("@Take", SqlDbType.Int).Value = Math.Clamp(take, 1, 1000);
        command.Parameters.Add("@BrandId", SqlDbType.Int).Value = brandId is null ? DBNull.Value : brandId.Value;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = status is null ? DBNull.Value : status;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<InventoryReportRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new InventoryReportRow
            {
                CarId = reader.GetInt32(0),
                CarName = reader.GetString(1),
                BrandName = reader.GetString(2),
                Status = reader.IsDBNull(3) ? CarStatusCatalog.InStock : reader.GetString(3),
                Price = reader.GetDecimal(4),
                StockQuantity = reader.GetInt32(5),
                Year = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                Type = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return rows;
    }

    private static async Task<IReadOnlyList<InventoryStatusSummaryRow>> ReadInventoryStatusSummaryAsync(
        SqlConnection connection,
        int? brandId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(InventoryStatusSummarySql, connection);
        command.Parameters.Add("@BrandId", SqlDbType.Int).Value = brandId is null ? DBNull.Value : brandId.Value;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<InventoryStatusSummaryRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new InventoryStatusSummaryRow
            {
                Status = reader.IsDBNull(0) ? "-" : reader.GetString(0),
                CarCount = reader.GetInt32(1),
                StockQuantity = reader.GetInt32(2)
            });
        }

        return rows;
    }

    private static async Task<string> ResolveBrandLabelAsync(SqlConnection connection, int? brandId, CancellationToken cancellationToken)
    {
        if (brandId is null)
        {
            return "Tat ca hang";
        }

        await using var command = new SqlCommand("SELECT TOP (1) Name FROM Brands WHERE Id = @Id;", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = brandId.Value;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? $"Brand #{brandId.Value}" : Convert.ToString(result) ?? $"Brand #{brandId.Value}";
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("ShowroomDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new FriendlyOperationException("Chua cau hinh connection string ShowroomDb.");
        }

        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            connection.Dispose();
            throw new FriendlyOperationException("Khong the ket noi toi SQL Server.", ex);
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

    public static byte[] ToCsvWithBom(string csvText)
    {
        var bom = Encoding.UTF8.GetPreamble();
        var bytes = Encoding.UTF8.GetBytes(csvText ?? string.Empty);
        var combined = new byte[bom.Length + bytes.Length];
        Buffer.BlockCopy(bom, 0, combined, 0, bom.Length);
        Buffer.BlockCopy(bytes, 0, combined, bom.Length, bytes.Length);
        return combined;
    }

    public static string CsvEscape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"', StringComparison.Ordinal))
        {
            text = text.Replace("\"", "\"\"", StringComparison.Ordinal);
        }

        if (text.Contains(',', StringComparison.Ordinal) || text.Contains('\n', StringComparison.Ordinal) || text.Contains('\r', StringComparison.Ordinal))
        {
            return $"\"{text}\"";
        }

        return text;
    }
}

