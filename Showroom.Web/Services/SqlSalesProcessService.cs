using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class SqlSalesProcessService : ISalesProcessService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlSalesProcessService> _logger;

    public SqlSalesProcessService(IConfiguration configuration, ILogger<SqlSalesProcessService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SalesProcessDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);

            var customerOptions = await ReadOptionsAsync(connection, """
                SELECT Id, FullName + N' (' + CustomerCode + N')'
                FROM Customers
                ORDER BY FullName;
                """, cancellationToken);
            var customerRequestOptions = await ReadOptionsAsync(connection, """
                SELECT TOP (50) Id, CustomerName + N' - ' + RequestType + N' - ' + Status
                FROM CustomerRequests
                ORDER BY CreatedAt DESC, Id DESC;
                """, cancellationToken);
            var staffOptions = await ReadOptionsAsync(connection, """
                SELECT Id, DisplayName + N' (' + Username + N')'
                FROM StaffUsers
                WHERE IsActive = 1
                ORDER BY DisplayName;
                """, cancellationToken);
            var modelOptions = await ReadOptionsAsync(connection, """
                SELECT Id, ModelName + N' ' + CONVERT(nvarchar(10), ModelYear) + N' (' + ModelCode + N')'
                FROM VehicleModels
                ORDER BY ModelName;
                """, cancellationToken);
            var leadOptions = await ReadOptionsAsync(connection, """
                SELECT Id, ContactName + N' - ' + Status
                FROM Leads
                WHERE Status NOT IN (N'Lost', N'Unqualified')
                ORDER BY CreatedAt DESC, Id DESC;
                """, cancellationToken);
            var opportunityOptions = await ReadOptionsAsync(connection, """
                SELECT Id, OpportunityName + N' - ' + Stage
                FROM SalesOpportunities
                WHERE Stage NOT IN (N'Won', N'Lost')
                ORDER BY CreatedAt DESC, Id DESC;
                """, cancellationToken);
            var vehicleOptions = await ReadOptionsAsync(connection, """
                SELECT nv.Id, vm.ModelName + N' - ' + nv.Vin + N' (' + nv.Status + N')'
                FROM NewVehicles nv
                INNER JOIN VehicleModels vm ON vm.Id = nv.VehicleModelId
                WHERE nv.Status NOT IN (N'Delivered', N'DamagedHold', N'ReturnedToFactory')
                ORDER BY nv.CreatedAt DESC, nv.Id DESC;
                """, cancellationToken);
            var contractOptions = await ReadOptionsAsync(connection, """
                SELECT Id, ContractNo + N' - ' + Status
                FROM SalesContracts
                WHERE Status NOT IN (N'Cancelled', N'Voided', N'Delivered')
                ORDER BY CreatedAt DESC, Id DESC;
                """, cancellationToken);
            var deliveryOptions = await ReadOptionsAsync(connection, """
                SELECT vd.Id, sc.ContractNo + N' - ' + nv.Vin
                FROM VehicleDeliveries vd
                INNER JOIN SalesContracts sc ON sc.Id = vd.SalesContractId
                INNER JOIN NewVehicles nv ON nv.Id = sc.NewVehicleId
                ORDER BY vd.CreatedAt DESC, vd.Id DESC;
                """, cancellationToken);

            return new SalesProcessDashboardViewModel
            {
                Leads = await ReadLeadsAsync(connection, cancellationToken),
                Opportunities = await ReadOpportunitiesAsync(connection, cancellationToken),
                TestDrives = await ReadTestDrivesAsync(connection, cancellationToken),
                Quotes = await ReadQuotesAsync(connection, cancellationToken),
                Contracts = await ReadContractsAsync(connection, cancellationToken),
                Accessories = await ReadAccessoriesAsync(connection, cancellationToken),
                RegistrationTasks = await ReadRegistrationTasksAsync(connection, cancellationToken),
                Deliveries = await ReadDeliveriesAsync(connection, cancellationToken),
                DeliveryChecklistItems = await ReadChecklistItemsAsync(connection, cancellationToken),
                LeadForm = new LeadFormViewModel
                {
                    CustomerOptions = customerOptions,
                    CustomerRequestOptions = customerRequestOptions,
                    StaffOptions = staffOptions,
                    ModelOptions = modelOptions,
                    SourceOptions = LeadSourceCatalog.GetSelectList(),
                    StatusOptions = LeadStatusCatalog.GetSelectList()
                },
                OpportunityForm = new SalesOpportunityFormViewModel
                {
                    LeadOptions = leadOptions,
                    CustomerOptions = customerOptions,
                    ModelOptions = modelOptions,
                    StaffOptions = staffOptions,
                    StageOptions = SalesStageCatalog.GetSelectList()
                },
                TestDriveForm = new TestDriveFormViewModel
                {
                    OpportunityOptions = opportunityOptions,
                    VehicleOptions = vehicleOptions,
                    StatusOptions = TestDriveStatusCatalog.GetSelectList()
                },
                QuoteForm = new SalesQuoteFormViewModel
                {
                    OpportunityOptions = opportunityOptions,
                    ModelOptions = modelOptions,
                    VehicleOptions = vehicleOptions,
                    StatusOptions = SalesQuoteStatusCatalog.GetSelectList()
                },
                ContractForm = new SalesContractFormViewModel
                {
                    OpportunityOptions = opportunityOptions,
                    CustomerOptions = customerOptions,
                    VehicleOptions = vehicleOptions,
                    StaffOptions = staffOptions,
                    PaymentMethodOptions = SalesPaymentMethodCatalog.GetSelectList(),
                    StatusOptions = SalesContractStatusCatalog.GetSelectList()
                },
                AccessoryForm = new ContractAccessoryFormViewModel { ContractOptions = contractOptions },
                RegistrationTaskForm = new RegistrationTaskFormViewModel
                {
                    ContractOptions = contractOptions,
                    StaffOptions = staffOptions,
                    TaskTypeOptions = RegistrationTaskTypeCatalog.GetSelectList(),
                    StatusOptions = RegistrationTaskStatusCatalog.GetSelectList()
                },
                DeliveryForm = new VehicleDeliveryFormViewModel { ContractOptions = contractOptions, StaffOptions = staffOptions },
                ChecklistItemForm = new DeliveryChecklistItemFormViewModel { DeliveryOptions = deliveryOptions }
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load sales process dashboard.");
            throw CreateFriendlyException("Không thể tải dữ liệu quy trình bán hàng.", ex);
        }
    }

    public async Task CreateLeadAsync(LeadFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Leads
                (CustomerId, CustomerRequestId, AssignedSalesStaffUserId, DesiredModelId, Source, ContactName, ContactPhone, ContactEmail, LeadScore, Status, Note)
            VALUES
                (@CustomerId, @CustomerRequestId, @AssignedSalesStaffUserId, @DesiredModelId, @Source, @ContactName, @ContactPhone, @ContactEmail, @LeadScore, @Status, @Note);
            """;

        try
        {
            EnsureContact(model.ContactPhone, model.ContactEmail);
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = ToDbNullable(model.CustomerId);
            command.Parameters.Add("@CustomerRequestId", SqlDbType.Int).Value = ToDbNullable(model.CustomerRequestId);
            command.Parameters.Add("@AssignedSalesStaffUserId", SqlDbType.Int).Value = ToDbNullable(model.AssignedSalesStaffUserId);
            command.Parameters.Add("@DesiredModelId", SqlDbType.Int).Value = ToDbNullable(model.DesiredModelId);
            command.Parameters.Add("@Source", SqlDbType.NVarChar, 30).Value = LeadSourceCatalog.Normalize(model.Source);
            command.Parameters.Add("@ContactName", SqlDbType.NVarChar, 150).Value = model.ContactName.Trim();
            command.Parameters.Add("@ContactPhone", SqlDbType.NVarChar, 30).Value = ToDbNullable(model.ContactPhone);
            command.Parameters.Add("@ContactEmail", SqlDbType.NVarChar, 254).Value = ToDbNullable(model.ContactEmail);
            command.Parameters.Add("@LeadScore", SqlDbType.TinyInt).Value = model.LeadScore;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = LeadStatusCatalog.Normalize(model.Status);
            command.Parameters.Add("@Note", SqlDbType.NVarChar, 500).Value = ToDbNullable(model.Note);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể tạo lead.", ex);
        }
    }

    public async Task CreateOpportunityAsync(SalesOpportunityFormViewModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string sql = """
                INSERT INTO SalesOpportunities
                    (LeadId, CustomerId, DesiredModelId, SalesStaffUserId, OpportunityName, Stage, ProbabilityPercent, ExpectedCloseDate)
                VALUES
                    (@LeadId, @CustomerId, @DesiredModelId, @SalesStaffUserId, @OpportunityName, @Stage, @ProbabilityPercent, @ExpectedCloseDate);

                UPDATE Leads
                SET Status = CASE WHEN Status = N'New' THEN N'Qualified' ELSE Status END
                WHERE Id = @LeadId;
                """;

            await using var command = new SqlCommand(sql, connection, (SqlTransaction)transaction);
            command.Parameters.Add("@LeadId", SqlDbType.Int).Value = model.LeadId;
            command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = ToDbNullable(model.CustomerId);
            command.Parameters.Add("@DesiredModelId", SqlDbType.Int).Value = ToDbNullable(model.DesiredModelId);
            command.Parameters.Add("@SalesStaffUserId", SqlDbType.Int).Value = ToDbNullable(model.SalesStaffUserId);
            command.Parameters.Add("@OpportunityName", SqlDbType.NVarChar, 150).Value = model.OpportunityName.Trim();
            command.Parameters.Add("@Stage", SqlDbType.NVarChar, 40).Value = SalesStageCatalog.Normalize(model.Stage);
            command.Parameters.Add("@ProbabilityPercent", SqlDbType.TinyInt).Value = model.ProbabilityPercent;
            command.Parameters.Add("@ExpectedCloseDate", SqlDbType.Date).Value = ToDbNullable(model.ExpectedCloseDate);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException("Không thể tạo cơ hội bán hàng.", ex);
        }
    }

    public async Task ScheduleTestDriveAsync(TestDriveFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO TestDrives (OpportunityId, NewVehicleId, ScheduledStartAt, Status, FeedbackNote)
            VALUES (@OpportunityId, @NewVehicleId, @ScheduledStartAt, @Status, @FeedbackNote);

            UPDATE SalesOpportunities
            SET Stage = N'TestDrive',
                ProbabilityPercent = CASE WHEN ProbabilityPercent < 30 THEN 30 ELSE ProbabilityPercent END
            WHERE Id = @OpportunityId;
            """;

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@OpportunityId", SqlDbType.Int).Value = model.OpportunityId;
            command.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = model.NewVehicleId;
            command.Parameters.Add("@ScheduledStartAt", SqlDbType.DateTime2).Value = model.ScheduledStartAt;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = TestDriveStatusCatalog.Normalize(model.Status);
            command.Parameters.Add("@FeedbackNote", SqlDbType.NVarChar, 500).Value = ToDbNullable(model.FeedbackNote);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể đặt lịch lái thử.", ex);
        }
    }

    public async Task CreateQuoteAsync(SalesQuoteFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO SalesQuotes
                (QuoteNo, OpportunityId, ModelId, NewVehicleId, VehiclePrice, DiscountAmount, RegistrationFeeEstimate, InsuranceEstimate, AccessoryPackageValue, ValidUntil, Status)
            VALUES
                (@QuoteNo, @OpportunityId, @ModelId, @NewVehicleId, @VehiclePrice, @DiscountAmount, @RegistrationFeeEstimate, @InsuranceEstimate, @AccessoryPackageValue, @ValidUntil, @Status);

            UPDATE SalesOpportunities
            SET Stage = N'Quote',
                ProbabilityPercent = CASE WHEN ProbabilityPercent < 50 THEN 50 ELSE ProbabilityPercent END
            WHERE Id = @OpportunityId;
            """;

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@QuoteNo", SqlDbType.NVarChar, 50).Value = model.QuoteNo.Trim().ToUpperInvariant();
            command.Parameters.Add("@OpportunityId", SqlDbType.Int).Value = model.OpportunityId;
            command.Parameters.Add("@ModelId", SqlDbType.Int).Value = model.ModelId;
            command.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = ToDbNullable(model.NewVehicleId);
            AddMoney(command, "@VehiclePrice", model.VehiclePrice);
            AddMoney(command, "@DiscountAmount", model.DiscountAmount);
            AddMoney(command, "@RegistrationFeeEstimate", model.RegistrationFeeEstimate);
            AddMoney(command, "@InsuranceEstimate", model.InsuranceEstimate);
            AddMoney(command, "@AccessoryPackageValue", model.AccessoryPackageValue);
            command.Parameters.Add("@ValidUntil", SqlDbType.Date).Value = ToDbNullable(model.ValidUntil);
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = SalesQuoteStatusCatalog.Normalize(model.Status);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Số báo giá đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể tạo báo giá.", ex);
        }
    }

    public async Task CreateContractAsync(SalesContractFormViewModel model, CancellationToken cancellationToken = default)
    {
        var status = SalesContractStatusCatalog.Normalize(model.Status);
        if (status == SalesContractStatusCatalog.Delivered)
        {
            throw CreateFriendlyException("Hãy tạo phiếu bàn giao để chuyển hợp đồng sang trạng thái đã bàn giao.");
        }

        var shouldLockVehicle = model.DepositAmount > 0 || SalesContractStatusCatalog.LocksVehicle(status);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var vehicleStatus = await ReadVehicleStatusForUpdateAsync(connection, (SqlTransaction)transaction, model.NewVehicleId, cancellationToken);
            if (vehicleStatus is null)
            {
                throw CreateFriendlyException("Không tìm thấy VIN cần lập hợp đồng.");
            }

            if (vehicleStatus is "Delivered" or "DamagedHold" or "ReturnedToFactory")
            {
                throw CreateFriendlyException("VIN này không sẵn sàng để lập hợp đồng.");
            }

            if (shouldLockVehicle && vehicleStatus is "Allocated_Locked" or "PendingDelivery")
            {
                throw CreateFriendlyException("VIN này đã được khóa cho một hợp đồng khác.");
            }

            if (await HasOpenContractAsync(connection, (SqlTransaction)transaction, model.NewVehicleId, cancellationToken))
            {
                throw CreateFriendlyException("VIN này đã có hợp đồng đang mở.");
            }

            const string insertSql = """
                INSERT INTO SalesContracts
                    (ContractNo, OpportunityId, CustomerId, NewVehicleId, SalesStaffUserId, FinalSalePrice, PaymentMethod, Status, DepositAmount, DepositDueAt, SignedAt)
                VALUES
                    (@ContractNo, @OpportunityId, @CustomerId, @NewVehicleId, @SalesStaffUserId, @FinalSalePrice, @PaymentMethod, @Status, @DepositAmount, @DepositDueAt, @SignedAt);

                UPDATE SalesOpportunities
                SET Stage = N'Contract',
                    ProbabilityPercent = CASE WHEN ProbabilityPercent < 75 THEN 75 ELSE ProbabilityPercent END
                WHERE Id = @OpportunityId;
                """;
            await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
            insertCommand.Parameters.Add("@ContractNo", SqlDbType.NVarChar, 50).Value = model.ContractNo.Trim().ToUpperInvariant();
            insertCommand.Parameters.Add("@OpportunityId", SqlDbType.Int).Value = model.OpportunityId;
            insertCommand.Parameters.Add("@CustomerId", SqlDbType.Int).Value = ToDbNullable(model.CustomerId);
            insertCommand.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = model.NewVehicleId;
            insertCommand.Parameters.Add("@SalesStaffUserId", SqlDbType.Int).Value = ToDbNullable(model.SalesStaffUserId);
            AddMoney(insertCommand, "@FinalSalePrice", model.FinalSalePrice);
            insertCommand.Parameters.Add("@PaymentMethod", SqlDbType.NVarChar, 30).Value = SalesPaymentMethodCatalog.Normalize(model.PaymentMethod);
            insertCommand.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = status;
            AddMoney(insertCommand, "@DepositAmount", model.DepositAmount);
            insertCommand.Parameters.Add("@DepositDueAt", SqlDbType.DateTime2).Value = ToDbNullable(model.DepositDueAt);
            insertCommand.Parameters.Add("@SignedAt", SqlDbType.DateTime2).Value = ToDbNullable(model.SignedAt);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);

            if (shouldLockVehicle)
            {
                const string lockSql = """
                    UPDATE NewVehicles
                    SET Status = N'Allocated_Locked'
                    WHERE Id = @NewVehicleId;
                    """;
                await using var lockCommand = new SqlCommand(lockSql, connection, (SqlTransaction)transaction);
                lockCommand.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = model.NewVehicleId;
                await lockCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException("Số hợp đồng đã tồn tại hoặc VIN đã có hợp đồng đang mở.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException(ex.Message, ex);
        }
    }

    public async Task AddAccessoryAsync(ContractAccessoryFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO ContractAccessories (SalesContractId, AccessoryName, Quantity, UnitPrice, UnitCost, IsGift)
            VALUES (@SalesContractId, @AccessoryName, @Quantity, @UnitPrice, @UnitCost, @IsGift);
            """;
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@SalesContractId", SqlDbType.Int).Value = model.SalesContractId;
            command.Parameters.Add("@AccessoryName", SqlDbType.NVarChar, 150).Value = model.AccessoryName.Trim();
            command.Parameters.Add("@Quantity", SqlDbType.Int).Value = model.Quantity;
            AddMoney(command, "@UnitPrice", model.UnitPrice);
            AddMoney(command, "@UnitCost", model.UnitCost);
            command.Parameters.Add("@IsGift", SqlDbType.Bit).Value = model.IsGift;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể thêm phụ kiện hợp đồng.", ex);
        }
    }

    public async Task CreateRegistrationTaskAsync(RegistrationTaskFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO RegistrationTasks (SalesContractId, TaskType, Status, DueDate, Amount, HandledByStaffUserId, Note)
            VALUES (@SalesContractId, @TaskType, @Status, @DueDate, @Amount, @HandledByStaffUserId, @Note);

            UPDATE SalesContracts
            SET Status = CASE WHEN Status IN (N'Draft', N'DepositPaid', N'Signed') THEN N'PendingRegistration' ELSE Status END,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @SalesContractId;
            """;
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@SalesContractId", SqlDbType.Int).Value = model.SalesContractId;
            command.Parameters.Add("@TaskType", SqlDbType.NVarChar, 40).Value = RegistrationTaskTypeCatalog.Normalize(model.TaskType);
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = RegistrationTaskStatusCatalog.Normalize(model.Status);
            command.Parameters.Add("@DueDate", SqlDbType.Date).Value = ToDbNullable(model.DueDate);
            AddMoney(command, "@Amount", model.Amount);
            command.Parameters.Add("@HandledByStaffUserId", SqlDbType.Int).Value = ToDbNullable(model.HandledByStaffUserId);
            command.Parameters.Add("@Note", SqlDbType.NVarChar, 300).Value = ToDbNullable(model.Note);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể tạo thủ tục đăng ký.", ex);
        }
    }

    public async Task CreateDeliveryAsync(VehicleDeliveryFormViewModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string contractSql = """
                SELECT NewVehicleId, Status
                FROM SalesContracts WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @SalesContractId;
                """;
            await using var contractCommand = new SqlCommand(contractSql, connection, (SqlTransaction)transaction);
            contractCommand.Parameters.Add("@SalesContractId", SqlDbType.Int).Value = model.SalesContractId;
            await using var reader = await contractCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw CreateFriendlyException("Không tìm thấy hợp đồng cần bàn giao.");
            }

            var newVehicleId = reader.GetInt32(0);
            var contractStatus = reader.GetString(1);
            await reader.CloseAsync();

            if (contractStatus is SalesContractStatusCatalog.Cancelled or SalesContractStatusCatalog.Voided)
            {
                throw CreateFriendlyException("Không thể bàn giao hợp đồng đã hủy hoặc vô hiệu.");
            }

            var deliveredAtExpression = model.CustomerAccepted && model.DeliveredAt is null
                ? "SYSUTCDATETIME()"
                : "@DeliveredAt";
            var deliveryStatus = model.CustomerAccepted ? SalesContractStatusCatalog.Delivered : SalesContractStatusCatalog.PendingDelivery;
            var vehicleStatus = model.CustomerAccepted ? "Delivered" : "PendingDelivery";

            var insertSql = $"""
                INSERT INTO VehicleDeliveries (SalesContractId, DeliveryStaffUserId, DeliveredAt, OdometerAtDelivery, CustomerAccepted, Note)
                VALUES (@SalesContractId, @DeliveryStaffUserId, {deliveredAtExpression}, @OdometerAtDelivery, @CustomerAccepted, @Note);

                UPDATE SalesContracts
                SET Status = @ContractStatus,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @SalesContractId;

                UPDATE NewVehicles
                SET Status = @VehicleStatus,
                    CurrentOdometer = CASE WHEN @OdometerAtDelivery > CurrentOdometer THEN @OdometerAtDelivery ELSE CurrentOdometer END
                WHERE Id = @NewVehicleId;
                """;
            await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
            insertCommand.Parameters.Add("@SalesContractId", SqlDbType.Int).Value = model.SalesContractId;
            insertCommand.Parameters.Add("@DeliveryStaffUserId", SqlDbType.Int).Value = ToDbNullable(model.DeliveryStaffUserId);
            insertCommand.Parameters.Add("@DeliveredAt", SqlDbType.DateTime2).Value = ToDbNullable(model.DeliveredAt);
            insertCommand.Parameters.Add("@OdometerAtDelivery", SqlDbType.Int).Value = model.OdometerAtDelivery;
            insertCommand.Parameters.Add("@CustomerAccepted", SqlDbType.Bit).Value = model.CustomerAccepted;
            insertCommand.Parameters.Add("@Note", SqlDbType.NVarChar, 500).Value = ToDbNullable(model.Note);
            insertCommand.Parameters.Add("@ContractStatus", SqlDbType.NVarChar, 30).Value = deliveryStatus;
            insertCommand.Parameters.Add("@VehicleStatus", SqlDbType.NVarChar, 30).Value = vehicleStatus;
            insertCommand.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = newVehicleId;
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException("Hợp đồng này đã có phiếu bàn giao.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException(ex.Message, ex);
        }
    }

    public async Task AddChecklistItemAsync(DeliveryChecklistItemFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO DeliveryChecklistItems (VehicleDeliveryId, ChecklistName, IsCompleted, CompletedAt, Note)
            VALUES (@VehicleDeliveryId, @ChecklistName, @IsCompleted, CASE WHEN @IsCompleted = 1 THEN SYSUTCDATETIME() ELSE NULL END, @Note);
            """;
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@VehicleDeliveryId", SqlDbType.Int).Value = model.VehicleDeliveryId;
            command.Parameters.Add("@ChecklistName", SqlDbType.NVarChar, 150).Value = model.ChecklistName.Trim();
            command.Parameters.Add("@IsCompleted", SqlDbType.Bit).Value = model.IsCompleted;
            command.Parameters.Add("@Note", SqlDbType.NVarChar, 300).Value = ToDbNullable(model.Note);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể thêm checklist bàn giao.", ex);
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

    private static async Task<IReadOnlyList<SelectListItem>> ReadOptionsAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<SelectListItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SelectListItem { Value = reader.GetInt32(0).ToString(), Text = reader.GetString(1) });
        }

        return items;
    }

    private static async Task<IReadOnlyList<LeadListItemViewModel>> ReadLeadsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (30) l.Id, l.ContactName, l.ContactPhone, l.ContactEmail, l.Source, l.Status, l.LeadScore,
                   vm.ModelName, su.DisplayName, l.CreatedAt
            FROM Leads l
            LEFT JOIN VehicleModels vm ON vm.Id = l.DesiredModelId
            LEFT JOIN StaffUsers su ON su.Id = l.AssignedSalesStaffUserId
            ORDER BY l.CreatedAt DESC, l.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<LeadListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new LeadListItemViewModel
            {
                Id = reader.GetInt32(0),
                ContactName = reader.GetString(1),
                ContactPhone = reader.IsDBNull(2) ? null : reader.GetString(2),
                ContactEmail = reader.IsDBNull(3) ? null : reader.GetString(3),
                Source = reader.GetString(4),
                Status = reader.GetString(5),
                LeadScore = reader.GetByte(6),
                DesiredModelName = reader.IsDBNull(7) ? null : reader.GetString(7),
                AssignedSalesName = reader.IsDBNull(8) ? null : reader.GetString(8),
                CreatedAt = reader.GetDateTime(9)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<SalesOpportunityListItemViewModel>> ReadOpportunitiesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (30) o.Id, o.OpportunityName, l.ContactName, c.FullName, vm.ModelName,
                   o.Stage, o.ProbabilityPercent, o.ExpectedCloseDate, o.CreatedAt
            FROM SalesOpportunities o
            INNER JOIN Leads l ON l.Id = o.LeadId
            LEFT JOIN Customers c ON c.Id = o.CustomerId
            LEFT JOIN VehicleModels vm ON vm.Id = o.DesiredModelId
            ORDER BY o.CreatedAt DESC, o.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<SalesOpportunityListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SalesOpportunityListItemViewModel
            {
                Id = reader.GetInt32(0),
                OpportunityName = reader.GetString(1),
                LeadName = reader.GetString(2),
                CustomerName = reader.IsDBNull(3) ? null : reader.GetString(3),
                DesiredModelName = reader.IsDBNull(4) ? null : reader.GetString(4),
                Stage = reader.GetString(5),
                ProbabilityPercent = reader.GetByte(6),
                ExpectedCloseDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                CreatedAt = reader.GetDateTime(8)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<TestDriveListItemViewModel>> ReadTestDrivesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20) td.Id, o.OpportunityName, nv.Vin, vm.ModelName, td.ScheduledStartAt, td.Status
            FROM TestDrives td
            INNER JOIN SalesOpportunities o ON o.Id = td.OpportunityId
            INNER JOIN NewVehicles nv ON nv.Id = td.NewVehicleId
            INNER JOIN VehicleModels vm ON vm.Id = nv.VehicleModelId
            ORDER BY td.ScheduledStartAt DESC, td.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<TestDriveListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new TestDriveListItemViewModel
            {
                Id = reader.GetInt32(0),
                OpportunityName = reader.GetString(1),
                Vin = reader.GetString(2),
                ModelName = reader.GetString(3),
                ScheduledStartAt = reader.GetDateTime(4),
                Status = reader.GetString(5)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<SalesQuoteListItemViewModel>> ReadQuotesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20) q.Id, q.QuoteNo, o.OpportunityName, vm.ModelName, nv.Vin,
                   q.VehiclePrice, q.DiscountAmount, q.RegistrationFeeEstimate, q.InsuranceEstimate,
                   q.AccessoryPackageValue, q.ValidUntil, q.Status
            FROM SalesQuotes q
            INNER JOIN SalesOpportunities o ON o.Id = q.OpportunityId
            INNER JOIN VehicleModels vm ON vm.Id = q.ModelId
            LEFT JOIN NewVehicles nv ON nv.Id = q.NewVehicleId
            ORDER BY q.CreatedAt DESC, q.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<SalesQuoteListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SalesQuoteListItemViewModel
            {
                Id = reader.GetInt32(0),
                QuoteNo = reader.GetString(1),
                OpportunityName = reader.GetString(2),
                ModelName = reader.GetString(3),
                Vin = reader.IsDBNull(4) ? null : reader.GetString(4),
                VehiclePrice = reader.GetDecimal(5),
                DiscountAmount = reader.GetDecimal(6),
                RegistrationFeeEstimate = reader.GetDecimal(7),
                InsuranceEstimate = reader.GetDecimal(8),
                AccessoryPackageValue = reader.GetDecimal(9),
                ValidUntil = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                Status = reader.GetString(11)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<SalesContractListItemViewModel>> ReadContractsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (30) sc.Id, sc.ContractNo, o.OpportunityName, c.FullName, nv.Vin, vm.ModelName,
                   sc.FinalSalePrice, sc.DepositAmount, sc.PaymentMethod, sc.Status, sc.CreatedAt
            FROM SalesContracts sc
            INNER JOIN SalesOpportunities o ON o.Id = sc.OpportunityId
            LEFT JOIN Customers c ON c.Id = sc.CustomerId
            INNER JOIN NewVehicles nv ON nv.Id = sc.NewVehicleId
            INNER JOIN VehicleModels vm ON vm.Id = nv.VehicleModelId
            ORDER BY sc.CreatedAt DESC, sc.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<SalesContractListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SalesContractListItemViewModel
            {
                Id = reader.GetInt32(0),
                ContractNo = reader.GetString(1),
                OpportunityName = reader.GetString(2),
                CustomerName = reader.IsDBNull(3) ? null : reader.GetString(3),
                Vin = reader.GetString(4),
                ModelName = reader.GetString(5),
                FinalSalePrice = reader.GetDecimal(6),
                DepositAmount = reader.GetDecimal(7),
                PaymentMethod = reader.GetString(8),
                Status = reader.GetString(9),
                CreatedAt = reader.GetDateTime(10)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<ContractAccessoryListItemViewModel>> ReadAccessoriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20) sc.ContractNo, ca.AccessoryName, ca.Quantity, ca.UnitPrice, ca.IsGift
            FROM ContractAccessories ca
            INNER JOIN SalesContracts sc ON sc.Id = ca.SalesContractId
            ORDER BY ca.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<ContractAccessoryListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ContractAccessoryListItemViewModel
            {
                ContractNo = reader.GetString(0),
                AccessoryName = reader.GetString(1),
                Quantity = reader.GetInt32(2),
                UnitPrice = reader.GetDecimal(3),
                IsGift = reader.GetBoolean(4)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<RegistrationTaskListItemViewModel>> ReadRegistrationTasksAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20) sc.ContractNo, rt.TaskType, rt.Status, rt.DueDate, rt.Amount, su.DisplayName
            FROM RegistrationTasks rt
            INNER JOIN SalesContracts sc ON sc.Id = rt.SalesContractId
            LEFT JOIN StaffUsers su ON su.Id = rt.HandledByStaffUserId
            ORDER BY rt.CreatedAt DESC, rt.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<RegistrationTaskListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new RegistrationTaskListItemViewModel
            {
                ContractNo = reader.GetString(0),
                TaskType = reader.GetString(1),
                Status = reader.GetString(2),
                DueDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                Amount = reader.GetDecimal(4),
                HandledByName = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<VehicleDeliveryListItemViewModel>> ReadDeliveriesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20) vd.Id, sc.ContractNo, nv.Vin, vm.ModelName, su.DisplayName,
                   vd.DeliveredAt, vd.OdometerAtDelivery, vd.CustomerAccepted
            FROM VehicleDeliveries vd
            INNER JOIN SalesContracts sc ON sc.Id = vd.SalesContractId
            INNER JOIN NewVehicles nv ON nv.Id = sc.NewVehicleId
            INNER JOIN VehicleModels vm ON vm.Id = nv.VehicleModelId
            LEFT JOIN StaffUsers su ON su.Id = vd.DeliveryStaffUserId
            ORDER BY vd.CreatedAt DESC, vd.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<VehicleDeliveryListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new VehicleDeliveryListItemViewModel
            {
                Id = reader.GetInt32(0),
                ContractNo = reader.GetString(1),
                Vin = reader.GetString(2),
                ModelName = reader.GetString(3),
                DeliveryStaffName = reader.IsDBNull(4) ? null : reader.GetString(4),
                DeliveredAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                OdometerAtDelivery = reader.GetInt32(6),
                CustomerAccepted = reader.GetBoolean(7)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<DeliveryChecklistItemListItemViewModel>> ReadChecklistItemsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20) sc.ContractNo, ci.ChecklistName, ci.IsCompleted, ci.CompletedAt
            FROM DeliveryChecklistItems ci
            INNER JOIN VehicleDeliveries vd ON vd.Id = ci.VehicleDeliveryId
            INNER JOIN SalesContracts sc ON sc.Id = vd.SalesContractId
            ORDER BY ci.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<DeliveryChecklistItemListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new DeliveryChecklistItemListItemViewModel
            {
                ContractNo = reader.GetString(0),
                ChecklistName = reader.GetString(1),
                IsCompleted = reader.GetBoolean(2),
                CompletedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
            });
        }

        return items;
    }

    private static async Task<string?> ReadVehicleStatusForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int newVehicleId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Status
            FROM NewVehicles WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @NewVehicleId;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = newVehicleId;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value as string;
    }

    private static async Task<bool> HasOpenContractAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int newVehicleId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM SalesContracts WITH (UPDLOCK, HOLDLOCK)
            WHERE NewVehicleId = @NewVehicleId
              AND Status NOT IN (N'Cancelled', N'Voided', N'Delivered');
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = newVehicleId;
        var count = (int)await command.ExecuteScalarAsync(cancellationToken)!;
        return count > 0;
    }

    private static void EnsureContact(string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
        {
            throw CreateFriendlyException("Lead cần có số điện thoại hoặc email.");
        }
    }

    private static void AddMoney(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value;
    }

    private static object ToDbNullable(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static object ToDbNullable(int? value) => value is null or <= 0 ? DBNull.Value : value.Value;

    private static object ToDbNullable(DateTime? value) => value.HasValue ? value.Value : DBNull.Value;

    private static bool IsDuplicateKey(SqlException ex) => ex.Number is 2601 or 2627;

    private static FriendlyOperationException CreateFriendlyException(string message, Exception? innerException = null)
    {
        if (innerException is SqlException { Number: 207 or 208 })
        {
            message = "Database schema chưa cập nhật. Hãy chạy `database/upgrade.sql` hoặc `database/setup.sql` rồi thử lại.";
        }

        return new FriendlyOperationException(message, innerException);
    }
}
