using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class SqlCustomerRequestService : ICustomerRequestService
{
    private const string CarOptionsSql = """
        SELECT
            c.Id,
            c.Name,
            b.Name
        FROM Cars c
        INNER JOIN Brands b ON b.Id = c.BrandId
        ORDER BY b.Name, c.Name;
        """;

    private const string EnsureSchemaSql = """
        IF OBJECT_ID(N'dbo.CustomerRequests', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.CustomerRequests
            (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                RequestType NVARCHAR(30) NOT NULL,
                CarId INT NULL,
                CustomerName NVARCHAR(150) NOT NULL,
                CustomerPhone NVARCHAR(30) NULL,
                CustomerEmail NVARCHAR(254) NULL,
                PreferredTime DATETIME2 NULL,
                DepositAmount DECIMAL(18,2) NULL,
                Note NVARCHAR(500) NULL,
                Status NVARCHAR(30) NOT NULL DEFAULT N'Pending',
                NotificationChannel NVARCHAR(30) NULL,
                NotificationSentAt DATETIME2 NULL,
                NotificationMessage NVARCHAR(500) NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                ConfirmedAt DATETIME2 NULL,
                CONSTRAINT CK_CustomerRequests_Type CHECK (RequestType IN (N'ViewCar', N'Consultation', N'Deposit', N'TestDrive')),
                CONSTRAINT CK_CustomerRequests_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled')),
                CONSTRAINT CK_CustomerRequests_CustomerName_NotBlank CHECK (LEN(LTRIM(RTRIM(CustomerName))) > 0),
                CONSTRAINT CK_CustomerRequests_Contact CHECK (CustomerPhone IS NOT NULL OR CustomerEmail IS NOT NULL),
                CONSTRAINT CK_CustomerRequests_DepositAmount CHECK (DepositAmount IS NULL OR DepositAmount >= 0)
            );
        END;

        IF OBJECT_ID(N'dbo.CustomerRequests', N'U') IS NOT NULL
           AND NOT EXISTS
           (
               SELECT 1
               FROM sys.foreign_keys
               WHERE parent_object_id = OBJECT_ID(N'dbo.CustomerRequests')
                 AND name = N'FK_CustomerRequests_Cars'
           )
        BEGIN
            ALTER TABLE dbo.CustomerRequests
            ADD CONSTRAINT FK_CustomerRequests_Cars FOREIGN KEY (CarId) REFERENCES dbo.Cars(Id);
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'IX_CustomerRequests_Status_CreatedAt'
              AND object_id = OBJECT_ID(N'dbo.CustomerRequests')
        )
        BEGIN
            CREATE INDEX IX_CustomerRequests_Status_CreatedAt ON dbo.CustomerRequests (Status, CreatedAt DESC);
        END;
        """;

    private const string InsertRequestSql = """
        INSERT INTO CustomerRequests
            (RequestType, CarId, CustomerName, CustomerPhone, CustomerEmail, PreferredTime, DepositAmount, Note, Status)
        OUTPUT INSERTED.Id
        VALUES
            (@RequestType, @CarId, @CustomerName, @CustomerPhone, @CustomerEmail, @PreferredTime, @DepositAmount, @Note, @Status);
        """;

    private const string RequestListSql = """
        SELECT
            r.Id,
            r.RequestType,
            r.CustomerName,
            r.CustomerPhone,
            r.CustomerEmail,
            CONCAT(b.Name, N' - ', c.Name) AS CarName,
            r.PreferredTime,
            r.DepositAmount,
            r.Status,
            r.NotificationChannel,
            r.NotificationSentAt,
            r.CreatedAt
        FROM CustomerRequests r
        LEFT JOIN Cars c ON c.Id = r.CarId
        LEFT JOIN Brands b ON b.Id = c.BrandId
        ORDER BY
            CASE WHEN r.Status = N'Pending' THEN 0 ELSE 1 END,
            r.CreatedAt DESC,
            r.Id DESC;
        """;

    private const string RequestDetailsSql = """
        SELECT
            r.Id,
            r.RequestType,
            r.CarId,
            CONCAT(b.Name, N' - ', c.Name) AS CarName,
            r.CustomerName,
            r.CustomerPhone,
            r.CustomerEmail,
            r.PreferredTime,
            r.DepositAmount,
            r.Note,
            r.Status,
            r.NotificationChannel,
            r.NotificationSentAt,
            r.NotificationMessage,
            r.CreatedAt,
            r.ConfirmedAt
        FROM CustomerRequests r
        LEFT JOIN Cars c ON c.Id = r.CarId
        LEFT JOIN Brands b ON b.Id = c.BrandId
        WHERE r.Id = @Id;
        """;

    private const string ConfirmRequestSql = """
        UPDATE CustomerRequests
        SET Status = @Status,
            ConfirmedAt = COALESCE(ConfirmedAt, SYSUTCDATETIME()),
            NotificationChannel = @NotificationChannel,
            NotificationSentAt = SYSUTCDATETIME(),
            NotificationMessage = @NotificationMessage
        WHERE Id = @Id
          AND Status = N'Pending';
        """;

    private const string CancelRequestSql = """
        UPDATE CustomerRequests
        SET Status = @Status
        WHERE Id = @Id
          AND Status = N'Pending';
        """;

    private readonly IConfiguration _configuration;
    private readonly ICustomerNotificationService _notificationService;
    private readonly ILogger<SqlCustomerRequestService> _logger;

    public SqlCustomerRequestService(
        IConfiguration configuration,
        ICustomerNotificationService notificationService,
        ILogger<SqlCustomerRequestService> logger)
    {
        _configuration = configuration;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<CustomerRequestFormViewModel> GetNewRequestAsync(
        int? carId,
        string? requestType,
        CancellationToken cancellationToken = default)
    {
        var model = new CustomerRequestFormViewModel
        {
            CarId = carId,
            RequestType = CustomerRequestCatalog.IsValidType(requestType ?? string.Empty)
                ? requestType!
                : CustomerRequestCatalog.ViewCar
        };

        await PopulateOptionsAsync(model, cancellationToken);
        return model;
    }

    public async Task<int> CreateRequestAsync(
        CustomerRequestFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRequest(model);

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(InsertRequestSql, connection);
            command.Parameters.Add("@RequestType", SqlDbType.NVarChar, 30).Value = model.RequestType;
            command.Parameters.Add("@CarId", SqlDbType.Int).Value = model.CarId is null or <= 0 ? DBNull.Value : model.CarId.Value;
            command.Parameters.Add("@CustomerName", SqlDbType.NVarChar, 150).Value = model.CustomerName.Trim();
            command.Parameters.Add("@CustomerPhone", SqlDbType.NVarChar, 30).Value = ToDbNullIfBlank(model.CustomerPhone);
            command.Parameters.Add("@CustomerEmail", SqlDbType.NVarChar, 254).Value = ToDbNullIfBlank(model.CustomerEmail);
            command.Parameters.Add("@PreferredTime", SqlDbType.DateTime2).Value = model.PreferredTime ?? (object)DBNull.Value;

            var depositParameter = command.Parameters.Add("@DepositAmount", SqlDbType.Decimal);
            depositParameter.Precision = 18;
            depositParameter.Scale = 2;
            depositParameter.Value = model.DepositAmount ?? (object)DBNull.Value;

            command.Parameters.Add("@Note", SqlDbType.NVarChar, 500).Value = ToDbNullIfBlank(model.Note);
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = CustomerRequestCatalog.Pending;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not create customer request.");
            throw CreateFriendlyException("Khong the tao yeu cau khach hang.", ex);
        }
    }

    public async Task<IReadOnlyList<CustomerRequestListItemViewModel>> GetRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(RequestListSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<CustomerRequestListItemViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new CustomerRequestListItemViewModel
                {
                    Id = reader.GetInt32(0),
                    RequestType = reader.GetString(1),
                    CustomerName = reader.GetString(2),
                    CustomerPhone = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CustomerEmail = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CarName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    PreferredTime = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    DepositAmount = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    Status = reader.GetString(8),
                    NotificationChannel = reader.IsDBNull(9) ? null : reader.GetString(9),
                    NotificationSentAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    CreatedAt = reader.GetDateTime(11)
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
            _logger.LogWarning(ex, "Could not load customer requests.");
            throw CreateFriendlyException("Khong the tai danh sach yeu cau khach hang.", ex);
        }
    }

    public async Task<CustomerRequestDetailsViewModel?> GetRequestAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            return await ReadRequestAsync(id, connection, cancellationToken);
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load customer request {RequestId}.", id);
            throw CreateFriendlyException("Khong the tai yeu cau khach hang.", ex);
        }
    }

    public async Task<CustomerRequestConfirmationResult> ConfirmAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            var request = await ReadRequestAsync(id, connection, cancellationToken);
            if (request is null)
            {
                return new CustomerRequestConfirmationResult { Message = "Khong tim thay yeu cau can xac nhan." };
            }

            if (request.Status != CustomerRequestCatalog.Pending)
            {
                return new CustomerRequestConfirmationResult { Message = "Chi co the xac nhan yeu cau dang cho xu ly." };
            }

            var notification = await _notificationService.NotifyConfirmedAsync(request, cancellationToken);

            await using var command = new SqlCommand(ConfirmRequestSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = CustomerRequestCatalog.Confirmed;
            command.Parameters.Add("@NotificationChannel", SqlDbType.NVarChar, 30).Value = notification.Channel;
            command.Parameters.Add("@NotificationMessage", SqlDbType.NVarChar, 500).Value = notification.Message;

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                return new CustomerRequestConfirmationResult { Message = "Yeu cau da duoc xu ly truoc do." };
            }

            return new CustomerRequestConfirmationResult
            {
                Success = true,
                NotificationChannel = notification.Channel,
                Message = $"Da xac nhan va gui thong bao qua {notification.Channel}."
            };
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not confirm customer request {RequestId}.", id);
            throw CreateFriendlyException("Khong the xac nhan yeu cau khach hang.", ex);
        }
    }

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(CancelRequestSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = CustomerRequestCatalog.Cancelled;
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not cancel customer request {RequestId}.", id);
            throw CreateFriendlyException("Khong the huy yeu cau khach hang.", ex);
        }
    }

    public async Task PopulateOptionsAsync(
        CustomerRequestFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(CarOptionsSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<SelectListItem>
            {
                new() { Value = string.Empty, Text = "Chua chon xe cu the" }
            };

            while (await reader.ReadAsync(cancellationToken))
            {
                var value = reader.GetInt32(0).ToString();
                items.Add(new SelectListItem
                {
                    Value = value,
                    Text = $"{reader.GetString(2)} - {reader.GetString(1)}",
                    Selected = string.Equals(value, model.CarId?.ToString(), StringComparison.Ordinal)
                });
            }

            model.CarOptions = items;
            model.RequestTypeOptions = CustomerRequestCatalog.GetTypeSelectList();
        }
        catch (FriendlyOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load customer request options.");
            throw CreateFriendlyException("Khong the tai danh sach xe cho form yeu cau.", ex);
        }
    }

    private async Task<CustomerRequestDetailsViewModel?> ReadRequestAsync(
        int id,
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(RequestDetailsSql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CustomerRequestDetailsViewModel
        {
            Id = reader.GetInt32(0),
            RequestType = reader.GetString(1),
            CarId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
            CarName = reader.IsDBNull(3) ? null : reader.GetString(3),
            CustomerName = reader.GetString(4),
            CustomerPhone = reader.IsDBNull(5) ? null : reader.GetString(5),
            CustomerEmail = reader.IsDBNull(6) ? null : reader.GetString(6),
            PreferredTime = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
            DepositAmount = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
            Note = reader.IsDBNull(9) ? null : reader.GetString(9),
            Status = reader.GetString(10),
            NotificationChannel = reader.IsDBNull(11) ? null : reader.GetString(11),
            NotificationSentAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
            NotificationMessage = reader.IsDBNull(13) ? null : reader.GetString(13),
            CreatedAt = reader.GetDateTime(14),
            ConfirmedAt = reader.IsDBNull(15) ? null : reader.GetDateTime(15)
        };
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
            await EnsureSchemaAsync(connection, cancellationToken);
            return connection;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await connection.DisposeAsync();
            throw CreateFriendlyException("Khong the ket noi toi SQL Server.", ex);
        }
    }

    private static async Task EnsureSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(EnsureSchemaSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void ValidateRequest(CustomerRequestFormViewModel model)
    {
        if (!CustomerRequestCatalog.IsValidType(model.RequestType))
        {
            throw CreateFriendlyException("Nhu cau khong hop le.");
        }

        if (string.IsNullOrWhiteSpace(model.CustomerName))
        {
            throw CreateFriendlyException("Ten khach hang khong duoc de trong.");
        }

        if (string.IsNullOrWhiteSpace(model.CustomerPhone) && string.IsNullOrWhiteSpace(model.CustomerEmail))
        {
            throw CreateFriendlyException("Vui long nhap so dien thoai hoac email.");
        }

        if (model.RequestType == CustomerRequestCatalog.Deposit && (model.DepositAmount is null or <= 0))
        {
            throw CreateFriendlyException("Vui long nhap so tien dat coc du kien.");
        }
    }

    private static object ToDbNullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static FriendlyOperationException CreateFriendlyException(string message, Exception? innerException = null)
        => new(message, innerException);
}
