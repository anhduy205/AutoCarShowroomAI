using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class SqlCoreManagementService : ICoreManagementService
{
    private const string BranchListSql = """
        SELECT
            b.Id,
            b.BranchCode,
            b.Name,
            b.Status,
            b.CreatedAt,
            COUNT(s.Id) AS StaffCount
        FROM Branches b
        LEFT JOIN StaffUsers s ON s.BranchId = b.Id
        GROUP BY b.Id, b.BranchCode, b.Name, b.Status, b.CreatedAt
        ORDER BY CASE WHEN b.Status = N'Active' THEN 0 ELSE 1 END, b.Name;
        """;

    private const string BranchByIdSql = """
        SELECT Id, BranchCode, Name, Status
        FROM Branches
        WHERE Id = @Id;
        """;

    private const string InsertBranchSql = """
        INSERT INTO Branches (BranchCode, Name, Status)
        VALUES (@BranchCode, @Name, @Status);
        """;

    private const string UpdateBranchSql = """
        UPDATE Branches
        SET BranchCode = @BranchCode,
            Name = @Name,
            Status = @Status
        WHERE Id = @Id;
        """;

    private const string DeleteBranchSql = """
        DELETE FROM Branches
        WHERE Id = @Id;
        """;

    private const string BranchOptionsSql = """
        SELECT Id, BranchCode, Name, Status
        FROM Branches
        ORDER BY CASE WHEN Status = N'Active' THEN 0 ELSE 1 END, Name;
        """;

    private const string CustomerListSql = """
        SELECT Id, CustomerCode, FullName, Phone, Email, TaxCode, Address, CustomerType, CreatedAt
        FROM Customers
        ORDER BY CreatedAt DESC, Id DESC;
        """;

    private const string CustomerByIdSql = """
        SELECT Id, CustomerCode, FullName, Phone, Email, TaxCode, Address, CustomerType
        FROM Customers
        WHERE Id = @Id;
        """;

    private const string InsertCustomerSql = """
        INSERT INTO Customers (CustomerCode, FullName, Phone, Email, TaxCode, Address, CustomerType)
        VALUES (@CustomerCode, @FullName, @Phone, @Email, @TaxCode, @Address, @CustomerType);
        """;

    private const string UpdateCustomerSql = """
        UPDATE Customers
        SET CustomerCode = @CustomerCode,
            FullName = @FullName,
            Phone = @Phone,
            Email = @Email,
            TaxCode = @TaxCode,
            Address = @Address,
            CustomerType = @CustomerType
        WHERE Id = @Id;
        """;

    private const string DeleteCustomerSql = """
        DELETE FROM Customers
        WHERE Id = @Id;
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlCoreManagementService> _logger;

    public SqlCoreManagementService(IConfiguration configuration, ILogger<SqlCoreManagementService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BranchListItemViewModel>> GetBranchesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(BranchListSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var branches = new List<BranchListItemViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                branches.Add(new BranchListItemViewModel
                {
                    Id = reader.GetInt32(0),
                    BranchCode = reader.GetString(1),
                    Name = reader.GetString(2),
                    Status = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4),
                    StaffCount = reader.GetInt32(5)
                });
            }

            return branches;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load branches.");
            throw CreateFriendlyException("Không thể tải danh sách chi nhánh.", ex);
        }
    }

    public async Task<BranchFormViewModel?> GetBranchAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(BranchByIdSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var status = reader.GetString(3);
            return new BranchFormViewModel
            {
                Id = reader.GetInt32(0),
                BranchCode = reader.GetString(1),
                Name = reader.GetString(2),
                Status = status,
                StatusOptions = BranchStatusCatalog.GetSelectList(status)
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load branch {BranchId}.", id);
            throw CreateFriendlyException("Không thể tải thông tin chi nhánh.", ex);
        }
    }

    public async Task CreateBranchAsync(BranchFormViewModel model, CancellationToken cancellationToken = default)
    {
        ValidateRequiredText(model.BranchCode, "Mã chi nhánh không được để trống.");
        ValidateRequiredText(model.Name, "Tên chi nhánh không được để trống.");

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(InsertBranchSql, connection);
            FillBranchParameters(command, model, includeId: false);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Mã chi nhánh đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not create branch {BranchCode}.", model.BranchCode);
            throw CreateFriendlyException("Không thể thêm chi nhánh.", ex);
        }
    }

    public async Task<bool> UpdateBranchAsync(BranchFormViewModel model, CancellationToken cancellationToken = default)
    {
        ValidateRequiredText(model.BranchCode, "Mã chi nhánh không được để trống.");
        ValidateRequiredText(model.Name, "Tên chi nhánh không được để trống.");

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(UpdateBranchSql, connection);
            FillBranchParameters(command, model, includeId: true);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Mã chi nhánh đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not update branch {BranchId}.", model.Id);
            throw CreateFriendlyException("Không thể cập nhật chi nhánh.", ex);
        }
    }

    public async Task<bool> DeleteBranchAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(DeleteBranchSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsReferenceConstraintViolation(ex))
        {
            throw CreateFriendlyException("Không thể xoá chi nhánh đang có nhân viên hoặc giao dịch liên quan.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not delete branch {BranchId}.", id);
            throw CreateFriendlyException("Không thể xoá chi nhánh.", ex);
        }
    }

    public async Task<IReadOnlyList<SelectListItem>> GetBranchOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(BranchOptionsSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var items = new List<SelectListItem>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var status = reader.GetString(3);
                var label = status == BranchStatusCatalog.Active
                    ? $"{reader.GetString(2)} ({reader.GetString(1)})"
                    : $"{reader.GetString(2)} ({reader.GetString(1)}) - tạm ngưng";

                items.Add(new SelectListItem
                {
                    Value = reader.GetInt32(0).ToString(),
                    Text = label
                });
            }

            return items;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load branch options.");
            throw CreateFriendlyException("Không thể tải danh sách chi nhánh.", ex);
        }
    }

    public async Task<IReadOnlyList<CustomerListItemViewModel>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(CustomerListSql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var customers = new List<CustomerListItemViewModel>();
            while (await reader.ReadAsync(cancellationToken))
            {
                customers.Add(new CustomerListItemViewModel
                {
                    Id = reader.GetInt32(0),
                    CustomerCode = reader.GetString(1),
                    FullName = reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TaxCode = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CustomerType = reader.GetString(7),
                    CreatedAt = reader.GetDateTime(8)
                });
            }

            return customers;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load customers.");
            throw CreateFriendlyException("Không thể tải danh sách khách hàng.", ex);
        }
    }

    public async Task<CustomerFormViewModel?> GetCustomerAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(CustomerByIdSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var customerType = reader.GetString(7);
            return new CustomerFormViewModel
            {
                Id = reader.GetInt32(0),
                CustomerCode = reader.GetString(1),
                FullName = reader.GetString(2),
                Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                TaxCode = reader.IsDBNull(5) ? null : reader.GetString(5),
                Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                CustomerType = customerType,
                CustomerTypeOptions = CustomerTypeCatalog.GetSelectList(customerType)
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load customer {CustomerId}.", id);
            throw CreateFriendlyException("Không thể tải thông tin khách hàng.", ex);
        }
    }

    public async Task CreateCustomerAsync(CustomerFormViewModel model, CancellationToken cancellationToken = default)
    {
        ValidateRequiredText(model.CustomerCode, "Mã khách hàng không được để trống.");
        ValidateRequiredText(model.FullName, "Tên khách hàng không được để trống.");

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(InsertCustomerSql, connection);
            FillCustomerParameters(command, model, includeId: false);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Mã khách hàng hoặc email đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not create customer {CustomerCode}.", model.CustomerCode);
            throw CreateFriendlyException("Không thể thêm khách hàng.", ex);
        }
    }

    public async Task<bool> UpdateCustomerAsync(CustomerFormViewModel model, CancellationToken cancellationToken = default)
    {
        ValidateRequiredText(model.CustomerCode, "Mã khách hàng không được để trống.");
        ValidateRequiredText(model.FullName, "Tên khách hàng không được để trống.");

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(UpdateCustomerSql, connection);
            FillCustomerParameters(command, model, includeId: true);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Mã khách hàng hoặc email đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not update customer {CustomerId}.", model.Id);
            throw CreateFriendlyException("Không thể cập nhật khách hàng.", ex);
        }
    }

    public async Task<bool> DeleteCustomerAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(DeleteCustomerSql, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (SqlException ex) when (IsReferenceConstraintViolation(ex))
        {
            throw CreateFriendlyException("Không thể xoá khách hàng đang có giao dịch liên quan.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not delete customer {CustomerId}.", id);
            throw CreateFriendlyException("Không thể xoá khách hàng.", ex);
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

    private static void FillBranchParameters(SqlCommand command, BranchFormViewModel model, bool includeId)
    {
        if (includeId)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
        }

        command.Parameters.Add("@BranchCode", SqlDbType.NVarChar, 30).Value = model.BranchCode.Trim();
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = model.Name.Trim();
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = BranchStatusCatalog.Normalize(model.Status);
    }

    private static void FillCustomerParameters(SqlCommand command, CustomerFormViewModel model, bool includeId)
    {
        if (includeId)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = model.Id;
        }

        command.Parameters.Add("@CustomerCode", SqlDbType.NVarChar, 30).Value = model.CustomerCode.Trim();
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = model.FullName.Trim();
        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = ToDbNullable(model.Phone);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = ToDbNullable(model.Email);
        command.Parameters.Add("@TaxCode", SqlDbType.NVarChar, 50).Value = ToDbNullable(model.TaxCode);
        command.Parameters.Add("@Address", SqlDbType.NVarChar, 300).Value = ToDbNullable(model.Address);
        command.Parameters.Add("@CustomerType", SqlDbType.NVarChar, 20).Value = CustomerTypeCatalog.Normalize(model.CustomerType);
    }

    private static object ToDbNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static bool IsDuplicateKey(SqlException ex) => ex.Number is 2601 or 2627;

    private static bool IsReferenceConstraintViolation(SqlException ex) => ex.Number == 547;

    private static FriendlyOperationException CreateFriendlyException(string message, Exception? innerException = null)
    {
        if (innerException is SqlException { Number: 207 or 208 })
        {
            message = "Database schema chưa cập nhật. Hãy chạy `database/upgrade.sql` hoặc `database/setup.sql` rồi thử lại.";
        }

        return new FriendlyOperationException(message, innerException);
    }

    private static void ValidateRequiredText(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw CreateFriendlyException(message);
        }
    }
}
