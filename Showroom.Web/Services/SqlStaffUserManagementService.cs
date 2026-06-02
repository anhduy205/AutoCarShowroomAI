using System.Data;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class SqlStaffUserManagementService : IStaffUserManagementService
{
    private const string StaffUserListSql = """
        SELECT
            s.Id,
            s.Username,
            s.PasswordHash,
            s.DisplayName,
            s.Role,
            s.CreatedAt,
            s.BranchId,
            s.StaffCode,
            s.Email,
            s.Phone,
            s.IsActive,
            b.BranchCode,
            b.Name
        FROM StaffUsers s
        LEFT JOIN Branches b ON b.Id = s.BranchId
        ORDER BY s.CreatedAt DESC, s.Id DESC;
        """;

    private const string StaffUserByIdSql = """
        SELECT
            s.Id,
            s.Username,
            s.PasswordHash,
            s.DisplayName,
            s.Role,
            s.CreatedAt,
            s.BranchId,
            s.StaffCode,
            s.Email,
            s.Phone,
            s.IsActive,
            b.BranchCode,
            b.Name
        FROM StaffUsers s
        LEFT JOIN Branches b ON b.Id = s.BranchId
        WHERE s.Id = @Id;
        """;

    private const string StaffUserByUsernameSql = """
        SELECT TOP (1)
            s.Id,
            s.Username,
            s.PasswordHash,
            s.DisplayName,
            s.Role,
            s.CreatedAt,
            s.BranchId,
            s.StaffCode,
            s.Email,
            s.Phone,
            s.IsActive,
            b.BranchCode,
            b.Name
        FROM StaffUsers s
        LEFT JOIN Branches b ON b.Id = s.BranchId
        WHERE s.Username = @Username
          AND s.IsActive = 1;
        """;

    private const string InsertStaffUserSql = """
        INSERT INTO StaffUsers (Username, PasswordHash, DisplayName, Role, BranchId, StaffCode, Email, Phone, IsActive)
        OUTPUT INSERTED.Id
        VALUES (@Username, @PasswordHash, @DisplayName, @Role, @BranchId, @StaffCode, @Email, @Phone, @IsActive);
        """;

    private const string UpdateStaffUserSql = """
        UPDATE StaffUsers
        SET Username = @Username,
            DisplayName = @DisplayName,
            Role = @Role,
            BranchId = @BranchId,
            StaffCode = @StaffCode,
            Email = @Email,
            Phone = @Phone,
            IsActive = @IsActive
        WHERE Id = @Id;
        """;

    private const string UpdateStaffUserWithPasswordSql = """
        UPDATE StaffUsers
        SET Username = @Username,
            PasswordHash = @PasswordHash,
            DisplayName = @DisplayName,
            Role = @Role,
            BranchId = @BranchId,
            StaffCode = @StaffCode,
            Email = @Email,
            Phone = @Phone,
            IsActive = @IsActive
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
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = request.BranchId is null ? DBNull.Value : request.BranchId.Value;
            command.Parameters.Add("@StaffCode", SqlDbType.NVarChar, 30).Value = ToDbNullable(request.StaffCode);
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = ToDbNullable(request.Email);
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = ToDbNullable(request.Phone);
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = request.IsActive;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw new FriendlyOperationException("Tên đăng nhập da ton tai. Hay chon ten khac.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not create staff user {Username}.", request.Username);
            throw new FriendlyOperationException(ToCreateFriendlyMessage(ex), ex);
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
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = request.BranchId is null ? DBNull.Value : request.BranchId.Value;
            command.Parameters.Add("@StaffCode", SqlDbType.NVarChar, 30).Value = ToDbNullable(request.StaffCode);
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = ToDbNullable(request.Email);
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = ToDbNullable(request.Phone);
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = request.IsActive;

            if (!string.IsNullOrWhiteSpace(request.PasswordHash))
            {
                command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = request.PasswordHash.Trim();
            }

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw new FriendlyOperationException("Tên đăng nhập da ton tai. Hay chon ten khac.", ex);
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
            CreatedAt = reader.GetDateTime(5),
            BranchId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            StaffCode = reader.IsDBNull(7) ? null : reader.GetString(7),
            Email = reader.IsDBNull(8) ? null : reader.GetString(8),
            Phone = reader.IsDBNull(9) ? null : reader.GetString(9),
            IsActive = reader.GetBoolean(10),
            BranchCode = reader.IsDBNull(11) ? null : reader.GetString(11),
            BranchName = reader.IsDBNull(12) ? null : reader.GetString(12)
        };

    private static void ValidateCreateRequest(StaffUserCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new FriendlyOperationException("Tên đăng nhập không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
        {
            throw new FriendlyOperationException("Mật khẩu chua duoc thiet lap.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new FriendlyOperationException("Tên hiển thị không được để trống.");
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
            throw new FriendlyOperationException("Tên đăng nhập không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new FriendlyOperationException("Tên hiển thị không được để trống.");
        }
    }

    private static object ToDbNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static bool IsDuplicateKey(SqlException ex) => ex.Number is 2601 or 2627;

    private static string ToCreateFriendlyMessage(Exception ex)
    {
        if (ex is SqlException sqlException)
        {
            return sqlException.Number switch
            {
                207 or 208 => "Database chua cap nhat bang/cot StaffUsers. Hay chay database/upgrade.sql roi thu lai.",
                515 => "Bang StaffUsers dang thieu default cho cot bat buoc (thuong la CreatedAt). Hay chay database/upgrade.sql roi thu lai.",
                2628 or 8152 => "Du lieu nhan vien vuot qua do dai cot trong database. Hay chay database/upgrade.sql de cap nhat schema StaffUsers.",
                547 => "Quyen nhan vien khong hop le. Chi chap nhan Administrator hoac Staff.",
                _ => $"Khong the tao tai khoan nhan vien. Ma loi SQL: {sqlException.Number}."
            };
        }

        return "Khong the tao tai khoan nhan vien.";
    }

    private static string NormalizeRole(string? role)
        => string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase)
            ? "Administrator"
            : "Staff";
}
