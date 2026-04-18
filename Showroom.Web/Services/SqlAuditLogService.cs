using System.Data;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public class SqlAuditLogService : IAuditLogService
{
    private const string InsertAuditLogSql = """
        INSERT INTO AuditLogs
        (
            Username,
            DisplayName,
            Role,
            Action,
            EntityType,
            EntityId,
            Description,
            IpAddress
        )
        VALUES
        (
            @Username,
            @DisplayName,
            @Role,
            @Action,
            @EntityType,
            @EntityId,
            @Description,
            @IpAddress
        );
        """;

    private const string RecentAuditLogsSql = """
        SELECT TOP (100)
            Id,
            Username,
            DisplayName,
            Role,
            Action,
            EntityType,
            EntityId,
            Description,
            IpAddress,
            CreatedAt
        FROM AuditLogs
        ORDER BY CreatedAt DESC, Id DESC;
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlAuditLogService> _logger;

    public SqlAuditLogService(IConfiguration configuration, ILogger<SqlAuditLogService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("ShowroomDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("Skipping audit log write because ShowroomDb connection string is missing.");
            return;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(InsertAuditLogSql, connection);
            command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = entry.Username;
            command.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 150).Value = entry.DisplayName;
            command.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = entry.Role;
            command.Parameters.Add("@Action", SqlDbType.NVarChar, 100).Value = entry.Action;
            command.Parameters.Add("@EntityType", SqlDbType.NVarChar, 100).Value = entry.EntityType;
            command.Parameters.Add("@EntityId", SqlDbType.Int).Value = entry.EntityId is null ? DBNull.Value : entry.EntityId.Value;
            command.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = entry.Description;
            command.Parameters.Add("@IpAddress", SqlDbType.NVarChar, 64).Value =
                string.IsNullOrWhiteSpace(entry.IpAddress) ? DBNull.Value : entry.IpAddress;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not write audit log entry {Action} for {Username}.", entry.Action, entry.Username);
        }
    }

    public async Task<IReadOnlyList<AuditLogListItemViewModel>> GetRecentLogsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(RecentAuditLogsSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<AuditLogListItemViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new AuditLogListItemViewModel
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    DisplayName = reader.GetString(2),
                    Role = reader.GetString(3),
                    Action = reader.GetString(4),
                    EntityType = reader.GetString(5),
                    EntityId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    Description = reader.GetString(7),
                    IpAddress = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    CreatedAt = reader.GetDateTime(9)
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
            _logger.LogWarning(ex, "Could not load audit log list.");
            throw CreateFriendlyException("Khong the tai nhat ky hoat dong tu SQL Server.", ex);
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

    private static FriendlyOperationException CreateFriendlyException(string message, Exception? innerException = null)
        => new(message, innerException);
}
