using System.Data;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class SqlStaffUserManagementService : IStaffUserManagementService
{
    private const string StaffUserListSql = """
        SELECT Id, Username, PasswordHash, DisplayName, Role, CreatedAt
        FROM StaffUsers
        ORDER BY CreatedAt DESC, Id DESC;
        """;

    private const string StaffUserByIdSql = """
        SELECT Id, Username, PasswordHash, DisplayName, Role, CreatedAt
        FROM StaffUsers
        WHERE Id = @Id;
        """;

    private const string StaffUserByUsernameSql = """
        SELECT TOP (1) Id, Username, PasswordHash, DisplayName, Role, CreatedAt
        FROM StaffUsers
        WHERE Username = @Username;
        """;

    private const string InsertStaffUserSql = """
        INSERT INTO StaffUsers (Username, PasswordHash, DisplayName, Role)
        OUTPUT INSERTED.Id
        VALUES (@Username, @PasswordHash, @DisplayName, @Role);
        """;

    private const string UpdateStaffUserSql = """
        UPDATE StaffUsers
        SET Username = @Username,
            DisplayName = @DisplayName,
            Role = @Role
        WHERE Id = @Id;
        """;

    private const string UpdateStaffUserWithPasswordSql = """
        UPDATE StaffUsers
        SET Username = @Username,
            PasswordHash = @PasswordHash,
            DisplayName = @DisplayName,
            Role = @Role
        WHERE Id = @Id;
        """;

    private const string DeleteStaffUserSql = """
        DELETE FROM StaffUsers
        WHERE Id = @Id;
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlStaffUserManagementService> _logger;

    public SqlStaffUserManagementService(IConfiguration configuration, ILogger<SqlStaffUserManagementService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StaffUser>> GetStaffUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(StaffUserListSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var users = new List<StaffUser>();
            while (await reader.ReadAsync(cancellationToken))
            {
                users.Add(ReadStaffUser(reader));
            }

            return users;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load staff users.");
            throw new FriendlyOperationException("Khong the tai danh sach nhan vien tu SQL Server.", ex);
        }
    }

    public async Task<StaffUser?> GetStaffUserAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(StaffUserByIdSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return ReadStaffUser(reader);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load staff user {StaffUserId}.", id);
            throw new FriendlyOperationException("Khong the tai thong tin nhan vien.", ex);
        }
    }

    public async Task<StaffUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(StaffUserByUsernameSql, connection);
            command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = username.Trim();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return ReadStaffUser(reader);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not lookup staff user {Username}.", username);
            throw new FriendlyOperationException("Khong the kiem tra tai khoan nhan vien.", ex);
        }
    }

    public async Task<int> CreateStaffUserAsync(StaffUserCreateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(InsertStaffUserSql, connection);

            command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = request.Username.Trim();
            command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = request.PasswordHash.Trim();
            command.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 150).Value = request.DisplayName.Trim();
            command.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = NormalizeRole(request.Role);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw new FriendlyOperationException("Ten dang nhap da ton tai. Hay chon ten khac.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not create staff user {Username}.", request.Username);
            throw new FriendlyOperationException("Khong the tao tai khoan nhan vien.", ex);
        }
    }

    public async Task<bool> UpdateStaffUserAsync(StaffUserUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUpdateRequest(request);

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            var sql = string.IsNullOrWhiteSpace(request.PasswordHash)
                ? UpdateStaffUserSql
                : UpdateStaffUserWithPasswordSql;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = request.Id;
            command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = request.Username.Trim();
            command.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 150).Value = request.DisplayName.Trim();
            command.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = NormalizeRole(request.Role);

            if (!string.IsNullOrWhiteSpace(request.PasswordHash))
            {
                command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = request.PasswordHash.Trim();
            }

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw new FriendlyOperationException("Ten dang nhap da ton tai. Hay chon ten khac.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not update staff user {StaffUserId}.", request.Id);
            throw new FriendlyOperationException("Khong the cap nhat tai khoan nhan vien.", ex);
        }
    }

    public async Task<bool> DeleteStaffUserAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(DeleteStaffUserSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not delete staff user {StaffUserId}.", id);
            throw new FriendlyOperationException("Khong the xoa tai khoan nhan vien.", ex);
        }
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

    private static StaffUser ReadStaffUser(SqlDataReader reader)
        => new()
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            DisplayName = reader.GetString(3),
            Role = reader.GetString(4),
            CreatedAt = reader.GetDateTime(5)
        };

    private static void ValidateCreateRequest(StaffUserCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new FriendlyOperationException("Ten dang nhap khong duoc de trong.");
        }

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
        {
            throw new FriendlyOperationException("Mat khau chua duoc thiet lap.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new FriendlyOperationException("Ten hien thi khong duoc de trong.");
        }
    }

    private static void ValidateUpdateRequest(StaffUserUpdateRequest request)
    {
        if (request.Id <= 0)
        {
            throw new FriendlyOperationException("Tai khoan nhan vien khong hop le.");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new FriendlyOperationException("Ten dang nhap khong duoc de trong.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new FriendlyOperationException("Ten hien thi khong duoc de trong.");
        }
    }

    private static bool IsDuplicateKey(SqlException ex) => ex.Number is 2601 or 2627;

    private static string NormalizeRole(string? role)
        => string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase)
            ? "Administrator"
            : "Staff";
}

