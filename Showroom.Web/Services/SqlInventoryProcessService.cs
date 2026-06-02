using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public sealed class SqlInventoryProcessService : IInventoryProcessService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlInventoryProcessService> _logger;

    public SqlInventoryProcessService(IConfiguration configuration, ILogger<SqlInventoryProcessService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<InventoryProcessDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);

            var brandOptions = await ReadOptionsAsync(connection, "SELECT Id, Name FROM Brands ORDER BY Name;", cancellationToken);
            var branchOptions = await ReadOptionsAsync(connection, "SELECT Id, Name + N' (' + BranchCode + N')' FROM Branches ORDER BY Name;", cancellationToken);
            var modelOptions = await ReadOptionsAsync(connection, "SELECT Id, ModelName + N' ' + CONVERT(nvarchar(10), ModelYear) + N' (' + ModelCode + N')' FROM VehicleModels ORDER BY ModelName;", cancellationToken);
            var factoryOrderItemOptions = await ReadOptionsAsync(connection, """
                SELECT foi.Id,
                       fo.FactoryOrderNo + N' - ' + vm.ModelName + N' còn ' + CONVERT(nvarchar(20), foi.Quantity - COUNT(nv.Id))
                FROM FactoryOrderItems foi
                INNER JOIN FactoryOrders fo ON fo.Id = foi.FactoryOrderId
                INNER JOIN VehicleModels vm ON vm.Id = foi.VehicleModelId
                LEFT JOIN NewVehicles nv ON nv.FactoryOrderItemId = foi.Id
                GROUP BY foi.Id, fo.FactoryOrderNo, vm.ModelName, foi.Quantity
                HAVING foi.Quantity - COUNT(nv.Id) > 0
                ORDER BY fo.FactoryOrderNo DESC;
                """, cancellationToken);
            var locationOptions = await ReadOptionsAsync(connection, "SELECT Id, LocationName + N' (' + LocationCode + N')' FROM Locations WHERE IsActive = 1 ORDER BY LocationName;", cancellationToken);
            var vehicleOptions = await ReadOptionsAsync(connection, """
                SELECT nv.Id, vm.ModelName + N' - ' + nv.Vin
                FROM NewVehicles nv
                INNER JOIN VehicleModels vm ON vm.Id = nv.VehicleModelId
                ORDER BY nv.CreatedAt DESC;
                """, cancellationToken);
            var partOptions = await ReadOptionsAsync(connection, "SELECT Id, PartName + N' (' + PartCode + N')' FROM Parts WHERE IsActive = 1 ORDER BY PartName;", cancellationToken);
            var partPurchaseOrderItemOptions = await ReadOptionsAsync(connection, """
                SELECT poi.Id,
                       po.PurchaseOrderNo + N' - ' + p.PartName + N' còn ' + CONVERT(nvarchar(20), poi.OrderedQuantity - poi.ReceivedQuantity)
                FROM PartPurchaseOrderItems poi
                INNER JOIN PartPurchaseOrders po ON po.Id = poi.PartPurchaseOrderId
                INNER JOIN Parts p ON p.Id = poi.PartId
                WHERE poi.OrderedQuantity > poi.ReceivedQuantity
                ORDER BY po.PurchaseOrderNo DESC;
                """, cancellationToken);

            return new InventoryProcessDashboardViewModel
            {
                VehicleModels = await ReadVehicleModelsAsync(connection, cancellationToken),
                NewVehicles = await ReadNewVehiclesAsync(connection, cancellationToken),
                Locations = await ReadLocationsAsync(connection, cancellationToken),
                FactoryOrders = await ReadFactoryOrdersAsync(connection, cancellationToken),
                Parts = await ReadPartsAsync(connection, cancellationToken),
                PartPurchaseOrders = await ReadPartPurchaseOrdersAsync(connection, cancellationToken),
                VehicleMovements = await ReadVehicleMovementsAsync(connection, cancellationToken),
                PartStockMovements = await ReadPartStockMovementsAsync(connection, cancellationToken),
                VehicleModelForm = new VehicleModelFormViewModel { BrandOptions = brandOptions },
                LocationForm = new LocationFormViewModel { BranchOptions = branchOptions, LocationTypeOptions = LocationTypeCatalog.GetSelectList() },
                NewVehicleForm = new NewVehicleFormViewModel { VehicleModelOptions = modelOptions, FactoryOrderItemOptions = factoryOrderItemOptions, LocationOptions = locationOptions, StatusOptions = NewVehicleStatusCatalog.GetSelectList() },
                VehicleMovementForm = new VehicleMovementFormViewModel { NewVehicleOptions = vehicleOptions, LocationOptions = locationOptions },
                FactoryOrderForm = new FactoryOrderFormViewModel { BranchOptions = branchOptions, VehicleModelOptions = modelOptions, StatusOptions = FactoryOrderStatusCatalog.GetSelectList() },
                PartInventoryAdjustmentForm = new PartInventoryAdjustmentFormViewModel { PartOptions = partOptions, LocationOptions = locationOptions },
                PartPurchaseOrderForm = new PartPurchaseOrderFormViewModel { BranchOptions = branchOptions, PartOptions = partOptions, StatusOptions = PartPurchaseOrderStatusCatalog.GetSelectList() },
                PartPurchaseReceiptForm = new PartPurchaseReceiptFormViewModel { PartPurchaseOrderItemOptions = partPurchaseOrderItemOptions, LocationOptions = locationOptions }
            };
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not load inventory process dashboard.");
            throw CreateFriendlyException("Không thể tải dữ liệu tồn kho chi tiết.", ex);
        }
    }

    public async Task CreateVehicleModelAsync(VehicleModelFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO VehicleModels (BrandId, ModelCode, ModelName, ModelYear, BasePrice)
            VALUES (@BrandId, @ModelCode, @ModelName, @ModelYear, @BasePrice);
            """;

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@BrandId", SqlDbType.Int).Value = model.BrandId;
            command.Parameters.Add("@ModelCode", SqlDbType.NVarChar, 50).Value = model.ModelCode.Trim().ToUpperInvariant();
            command.Parameters.Add("@ModelName", SqlDbType.NVarChar, 150).Value = model.ModelName.Trim();
            command.Parameters.Add("@ModelYear", SqlDbType.SmallInt).Value = model.ModelYear;
            AddMoney(command, "@BasePrice", model.BasePrice);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Mã mẫu xe đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể thêm mẫu xe.", ex);
        }
    }

    public async Task CreateLocationAsync(LocationFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Locations (BranchId, LocationCode, LocationName, LocationType, IsActive)
            VALUES (@BranchId, @LocationCode, @LocationName, @LocationType, @IsActive);
            """;

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = model.BranchId;
            command.Parameters.Add("@LocationCode", SqlDbType.NVarChar, 50).Value = model.LocationCode.Trim().ToUpperInvariant();
            command.Parameters.Add("@LocationName", SqlDbType.NVarChar, 150).Value = model.LocationName.Trim();
            command.Parameters.Add("@LocationType", SqlDbType.NVarChar, 30).Value = LocationTypeCatalog.Normalize(model.LocationType);
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = model.IsActive;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Mã vị trí đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể thêm vị trí kho.", ex);
        }
    }

    public async Task CreateNewVehicleAsync(NewVehicleFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO NewVehicles
            (VehicleModelId, FactoryOrderItemId, CurrentLocationId, Vin, EngineNumber, ExteriorColor, InteriorColor, CostPrice, Msrp, CurrentOdometer, Status, InboundCheckedAt, InboundDamageNote)
            VALUES
            (@VehicleModelId, @FactoryOrderItemId, @CurrentLocationId, @Vin, @EngineNumber, @ExteriorColor, @InteriorColor, @CostPrice, @Msrp, @CurrentOdometer, @Status, SYSUTCDATETIME(), @InboundDamageNote);
            """;

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@VehicleModelId", SqlDbType.Int).Value = model.VehicleModelId;
            command.Parameters.Add("@FactoryOrderItemId", SqlDbType.Int).Value = model.FactoryOrderItemId is null ? DBNull.Value : model.FactoryOrderItemId.Value;
            command.Parameters.Add("@CurrentLocationId", SqlDbType.Int).Value = model.CurrentLocationId is null ? DBNull.Value : model.CurrentLocationId.Value;
            command.Parameters.Add("@Vin", SqlDbType.Char, 17).Value = model.Vin.Trim().ToUpperInvariant();
            command.Parameters.Add("@EngineNumber", SqlDbType.NVarChar, 50).Value = model.EngineNumber.Trim();
            command.Parameters.Add("@ExteriorColor", SqlDbType.NVarChar, 80).Value = ToDbNullable(model.ExteriorColor);
            command.Parameters.Add("@InteriorColor", SqlDbType.NVarChar, 80).Value = ToDbNullable(model.InteriorColor);
            AddMoney(command, "@CostPrice", model.CostPrice);
            AddMoney(command, "@Msrp", model.Msrp);
            command.Parameters.Add("@CurrentOdometer", SqlDbType.Int).Value = model.CurrentOdometer;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = NewVehicleStatusCatalog.Normalize(model.Status);
            command.Parameters.Add("@InboundDamageNote", SqlDbType.NVarChar, 1000).Value = ToDbNullable(model.InboundDamageNote);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("VIN hoặc số máy đã tồn tại.", ex);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw CreateFriendlyException("Mẫu xe hoặc vị trí được chọn không hợp lệ.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể thêm xe theo VIN.", ex);
        }
    }

    public async Task MoveVehicleAsync(VehicleMovementFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string currentSql = "SELECT CurrentLocationId FROM NewVehicles WHERE Id = @Id;";
        const string insertSql = """
            INSERT INTO VehicleLocationMovements (NewVehicleId, FromLocationId, ToLocationId, Reason)
            VALUES (@NewVehicleId, @FromLocationId, @ToLocationId, @Reason);
            """;
        const string updateSql = """
            UPDATE nv
            SET CurrentLocationId = @ToLocationId,
                Status = CASE WHEN loc.LocationType = N'Showroom' AND nv.Status = N'Ready' THEN N'OnDisplay' ELSE nv.Status END
            FROM NewVehicles nv
            INNER JOIN Locations loc ON loc.Id = @ToLocationId
            WHERE nv.Id = @NewVehicleId;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var currentCommand = new SqlCommand(currentSql, connection, (SqlTransaction)transaction);
            currentCommand.Parameters.Add("@Id", SqlDbType.Int).Value = model.NewVehicleId;
            var current = await currentCommand.ExecuteScalarAsync(cancellationToken);
            if (current is null)
            {
                throw CreateFriendlyException("Không tìm thấy xe cần chuyển vị trí.");
            }

            await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
            insertCommand.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = model.NewVehicleId;
            insertCommand.Parameters.Add("@FromLocationId", SqlDbType.Int).Value = current is DBNull ? DBNull.Value : current;
            insertCommand.Parameters.Add("@ToLocationId", SqlDbType.Int).Value = model.ToLocationId;
            insertCommand.Parameters.Add("@Reason", SqlDbType.NVarChar, 300).Value = ToDbNullable(model.Reason);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);

            await using var updateCommand = new SqlCommand(updateSql, connection, (SqlTransaction)transaction);
            updateCommand.Parameters.Add("@NewVehicleId", SqlDbType.Int).Value = model.NewVehicleId;
            updateCommand.Parameters.Add("@ToLocationId", SqlDbType.Int).Value = model.ToLocationId;
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException("Không thể chuyển vị trí xe.", ex);
        }
    }

    public async Task CreateFactoryOrderAsync(FactoryOrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string orderSql = """
                INSERT INTO FactoryOrders (FactoryOrderNo, BranchId, OrderDate, ExpectedArrivalDate, Status)
                OUTPUT INSERTED.Id
                VALUES (@FactoryOrderNo, @BranchId, @OrderDate, @ExpectedArrivalDate, @Status);
                """;
            await using var orderCommand = new SqlCommand(orderSql, connection, (SqlTransaction)transaction);
            orderCommand.Parameters.Add("@FactoryOrderNo", SqlDbType.NVarChar, 50).Value = model.FactoryOrderNo.Trim().ToUpperInvariant();
            orderCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value = model.BranchId;
            orderCommand.Parameters.Add("@OrderDate", SqlDbType.Date).Value = model.OrderDate.Date;
            orderCommand.Parameters.Add("@ExpectedArrivalDate", SqlDbType.Date).Value = model.ExpectedArrivalDate is null ? DBNull.Value : model.ExpectedArrivalDate.Value.Date;
            orderCommand.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = FactoryOrderStatusCatalog.Normalize(model.Status);
            var id = Convert.ToInt32(await orderCommand.ExecuteScalarAsync(cancellationToken));

            const string itemSql = """
                INSERT INTO FactoryOrderItems (FactoryOrderId, VehicleModelId, Quantity, PlannedUnitCost)
                VALUES (@FactoryOrderId, @VehicleModelId, @Quantity, @PlannedUnitCost);
                """;
            await using var itemCommand = new SqlCommand(itemSql, connection, (SqlTransaction)transaction);
            itemCommand.Parameters.Add("@FactoryOrderId", SqlDbType.Int).Value = id;
            itemCommand.Parameters.Add("@VehicleModelId", SqlDbType.Int).Value = model.VehicleModelId;
            itemCommand.Parameters.Add("@Quantity", SqlDbType.Int).Value = model.Quantity;
            AddMoney(itemCommand, "@PlannedUnitCost", model.PlannedUnitCost);
            await itemCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException(IsDuplicateKey(ex as SqlException) ? "Số đơn nhà máy đã tồn tại." : "Không thể tạo đơn nhà máy.", ex);
        }
    }

    public async Task CreatePartAsync(PartFormViewModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Parts (PartCode, PartName, Unit, ListPrice, IsActive)
            VALUES (@PartCode, @PartName, @Unit, @ListPrice, @IsActive);
            """;

        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@PartCode", SqlDbType.NVarChar, 50).Value = model.PartCode.Trim().ToUpperInvariant();
            command.Parameters.Add("@PartName", SqlDbType.NVarChar, 150).Value = model.PartName.Trim();
            command.Parameters.Add("@Unit", SqlDbType.NVarChar, 20).Value = model.Unit.Trim();
            AddMoney(command, "@ListPrice", model.ListPrice);
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = model.IsActive;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsDuplicateKey(ex))
        {
            throw CreateFriendlyException("Mã phụ tùng đã tồn tại.", ex);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            throw CreateFriendlyException("Không thể thêm phụ tùng.", ex);
        }
    }

    public async Task AdjustPartInventoryAsync(PartInventoryAdjustmentFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.QuantityDelta == 0)
        {
            throw CreateFriendlyException("Số lượng tăng/giảm tồn không được bằng 0.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string currentSql = """
                SELECT QuantityOnHand
                FROM PartInventory WITH (UPDLOCK, HOLDLOCK)
                WHERE PartId = @PartId AND LocationId = @LocationId;
                """;
            await using var currentCommand = new SqlCommand(currentSql, connection, (SqlTransaction)transaction);
            currentCommand.Parameters.Add("@PartId", SqlDbType.Int).Value = model.PartId;
            currentCommand.Parameters.Add("@LocationId", SqlDbType.Int).Value = model.LocationId;
            var currentResult = await currentCommand.ExecuteScalarAsync(cancellationToken);
            var currentQuantity = currentResult is null || currentResult is DBNull ? 0 : Convert.ToDecimal(currentResult);
            var nextQuantity = currentQuantity + model.QuantityDelta;
            if (nextQuantity < 0)
            {
                throw CreateFriendlyException("Không thể điều chỉnh tồn kho xuống dưới 0.");
            }

            const string upsertSql = """
                MERGE PartInventory AS target
                USING (SELECT @PartId AS PartId, @LocationId AS LocationId) AS source
                ON target.PartId = source.PartId AND target.LocationId = source.LocationId
                WHEN MATCHED THEN
                    UPDATE SET QuantityOnHand = @NextQuantity, MinStockLevel = @MinStockLevel, UpdatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (PartId, LocationId, QuantityOnHand, MinStockLevel)
                    VALUES (@PartId, @LocationId, @NextQuantity, @MinStockLevel);
                """;
            await using var upsertCommand = new SqlCommand(upsertSql, connection, (SqlTransaction)transaction);
            upsertCommand.Parameters.Add("@PartId", SqlDbType.Int).Value = model.PartId;
            upsertCommand.Parameters.Add("@LocationId", SqlDbType.Int).Value = model.LocationId;
            AddDecimal(upsertCommand, "@NextQuantity", nextQuantity);
            AddDecimal(upsertCommand, "@MinStockLevel", model.MinStockLevel);
            await upsertCommand.ExecuteNonQueryAsync(cancellationToken);

            const string movementSql = """
                INSERT INTO PartStockMovements (PartId, LocationId, MovementType, QuantityDelta, Note)
                VALUES (@PartId, @LocationId, N'Adjust', @QuantityDelta, @Note);
                """;
            await using var movementCommand = new SqlCommand(movementSql, connection, (SqlTransaction)transaction);
            movementCommand.Parameters.Add("@PartId", SqlDbType.Int).Value = model.PartId;
            movementCommand.Parameters.Add("@LocationId", SqlDbType.Int).Value = model.LocationId;
            AddDecimal(movementCommand, "@QuantityDelta", model.QuantityDelta);
            movementCommand.Parameters.Add("@Note", SqlDbType.NVarChar, 300).Value = ToDbNullable(model.Note);
            await movementCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException(ex.Message, ex);
        }
    }

    public async Task CreatePartPurchaseOrderAsync(PartPurchaseOrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string orderSql = """
                INSERT INTO PartPurchaseOrders (PurchaseOrderNo, BranchId, SupplierName, OrderDate, ExpectedArrivalDate, Status)
                OUTPUT INSERTED.Id
                VALUES (@PurchaseOrderNo, @BranchId, @SupplierName, @OrderDate, @ExpectedArrivalDate, @Status);
                """;
            await using var orderCommand = new SqlCommand(orderSql, connection, (SqlTransaction)transaction);
            orderCommand.Parameters.Add("@PurchaseOrderNo", SqlDbType.NVarChar, 50).Value = model.PurchaseOrderNo.Trim().ToUpperInvariant();
            orderCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value = model.BranchId;
            orderCommand.Parameters.Add("@SupplierName", SqlDbType.NVarChar, 150).Value = model.SupplierName.Trim();
            orderCommand.Parameters.Add("@OrderDate", SqlDbType.Date).Value = model.OrderDate.Date;
            orderCommand.Parameters.Add("@ExpectedArrivalDate", SqlDbType.Date).Value = model.ExpectedArrivalDate is null ? DBNull.Value : model.ExpectedArrivalDate.Value.Date;
            orderCommand.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = PartPurchaseOrderStatusCatalog.Normalize(model.Status);
            var id = Convert.ToInt32(await orderCommand.ExecuteScalarAsync(cancellationToken));

            const string itemSql = """
                INSERT INTO PartPurchaseOrderItems (PartPurchaseOrderId, PartId, OrderedQuantity, UnitCost)
                VALUES (@PartPurchaseOrderId, @PartId, @OrderedQuantity, @UnitCost);
                """;
            await using var itemCommand = new SqlCommand(itemSql, connection, (SqlTransaction)transaction);
            itemCommand.Parameters.Add("@PartPurchaseOrderId", SqlDbType.Int).Value = id;
            itemCommand.Parameters.Add("@PartId", SqlDbType.Int).Value = model.PartId;
            AddDecimal(itemCommand, "@OrderedQuantity", model.OrderedQuantity);
            AddMoney(itemCommand, "@UnitCost", model.UnitCost);
            await itemCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException(IsDuplicateKey(ex as SqlException) ? "Số đơn phụ tùng đã tồn tại." : "Không thể tạo đơn phụ tùng.", ex);
        }
    }

    public async Task ReceivePartPurchaseOrderItemAsync(PartPurchaseReceiptFormViewModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string itemSql = """
                SELECT PartPurchaseOrderId, PartId, OrderedQuantity, ReceivedQuantity
                FROM PartPurchaseOrderItems WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @Id;
                """;
            await using var itemCommand = new SqlCommand(itemSql, connection, (SqlTransaction)transaction);
            itemCommand.Parameters.Add("@Id", SqlDbType.Int).Value = model.PartPurchaseOrderItemId;
            await using var reader = await itemCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw CreateFriendlyException("Không tìm thấy dòng đơn phụ tùng.");
            }

            var orderId = reader.GetInt32(0);
            var partId = reader.GetInt32(1);
            var orderedQuantity = reader.GetDecimal(2);
            var receivedQuantity = reader.GetDecimal(3);
            await reader.CloseAsync();

            if (receivedQuantity + model.ReceivedQuantity > orderedQuantity)
            {
                throw CreateFriendlyException("Số lượng nhận vượt quá số lượng còn lại của đơn.");
            }

            const string updateItemSql = """
                UPDATE PartPurchaseOrderItems
                SET ReceivedQuantity = ReceivedQuantity + @ReceivedQuantity
                WHERE Id = @Id;
                """;
            await using var updateItemCommand = new SqlCommand(updateItemSql, connection, (SqlTransaction)transaction);
            updateItemCommand.Parameters.Add("@Id", SqlDbType.Int).Value = model.PartPurchaseOrderItemId;
            AddDecimal(updateItemCommand, "@ReceivedQuantity", model.ReceivedQuantity);
            await updateItemCommand.ExecuteNonQueryAsync(cancellationToken);

            const string upsertInventorySql = """
                MERGE PartInventory AS target
                USING (SELECT @PartId AS PartId, @LocationId AS LocationId) AS source
                ON target.PartId = source.PartId AND target.LocationId = source.LocationId
                WHEN MATCHED THEN
                    UPDATE SET QuantityOnHand = QuantityOnHand + @ReceivedQuantity, UpdatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (PartId, LocationId, QuantityOnHand, MinStockLevel)
                    VALUES (@PartId, @LocationId, @ReceivedQuantity, 0);
                """;
            await using var upsertInventoryCommand = new SqlCommand(upsertInventorySql, connection, (SqlTransaction)transaction);
            upsertInventoryCommand.Parameters.Add("@PartId", SqlDbType.Int).Value = partId;
            upsertInventoryCommand.Parameters.Add("@LocationId", SqlDbType.Int).Value = model.LocationId;
            AddDecimal(upsertInventoryCommand, "@ReceivedQuantity", model.ReceivedQuantity);
            await upsertInventoryCommand.ExecuteNonQueryAsync(cancellationToken);

            const string movementSql = """
                INSERT INTO PartStockMovements (PartId, LocationId, MovementType, QuantityDelta, PartPurchaseOrderItemId, Note)
                VALUES (@PartId, @LocationId, N'Receive', @ReceivedQuantity, @PartPurchaseOrderItemId, @Note);
                """;
            await using var movementCommand = new SqlCommand(movementSql, connection, (SqlTransaction)transaction);
            movementCommand.Parameters.Add("@PartId", SqlDbType.Int).Value = partId;
            movementCommand.Parameters.Add("@LocationId", SqlDbType.Int).Value = model.LocationId;
            movementCommand.Parameters.Add("@PartPurchaseOrderItemId", SqlDbType.Int).Value = model.PartPurchaseOrderItemId;
            AddDecimal(movementCommand, "@ReceivedQuantity", model.ReceivedQuantity);
            movementCommand.Parameters.Add("@Note", SqlDbType.NVarChar, 300).Value = ToDbNullable(model.Note);
            await movementCommand.ExecuteNonQueryAsync(cancellationToken);

            const string updateOrderStatusSql = """
                UPDATE po
                SET Status =
                    CASE
                        WHEN NOT EXISTS
                        (
                            SELECT 1
                            FROM PartPurchaseOrderItems item
                            WHERE item.PartPurchaseOrderId = po.Id
                              AND item.ReceivedQuantity < item.OrderedQuantity
                        ) THEN N'Received'
                        ELSE N'PartiallyReceived'
                    END
                FROM PartPurchaseOrders po
                WHERE po.Id = @OrderId;
                """;
            await using var updateOrderCommand = new SqlCommand(updateOrderStatusSql, connection, (SqlTransaction)transaction);
            updateOrderCommand.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;
            await updateOrderCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CreateFriendlyException(ex.Message, ex);
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

    private static async Task<IReadOnlyList<VehicleModelListItemViewModel>> ReadVehicleModelsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (50) vm.Id, b.Name, vm.ModelCode, vm.ModelName, vm.ModelYear, vm.BasePrice, COUNT(nv.Id)
            FROM VehicleModels vm
            INNER JOIN Brands b ON b.Id = vm.BrandId
            LEFT JOIN NewVehicles nv ON nv.VehicleModelId = vm.Id
            GROUP BY vm.Id, b.Name, vm.ModelCode, vm.ModelName, vm.ModelYear, vm.BasePrice
            ORDER BY vm.ModelYear DESC, vm.ModelName;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<VehicleModelListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new VehicleModelListItemViewModel
            {
                Id = reader.GetInt32(0),
                BrandName = reader.GetString(1),
                ModelCode = reader.GetString(2),
                ModelName = reader.GetString(3),
                ModelYear = reader.GetInt16(4),
                BasePrice = reader.GetDecimal(5),
                VehicleCount = reader.GetInt32(6)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<NewVehicleListItemViewModel>> ReadNewVehiclesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (50) nv.Id, vm.ModelName, nv.Vin, nv.EngineNumber, loc.LocationName, nv.ExteriorColor, nv.Msrp, nv.CurrentOdometer, nv.Status
            FROM NewVehicles nv
            INNER JOIN VehicleModels vm ON vm.Id = nv.VehicleModelId
            LEFT JOIN Locations loc ON loc.Id = nv.CurrentLocationId
            ORDER BY nv.CreatedAt DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<NewVehicleListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new NewVehicleListItemViewModel
            {
                Id = reader.GetInt32(0),
                ModelName = reader.GetString(1),
                Vin = reader.GetString(2),
                EngineNumber = reader.GetString(3),
                CurrentLocationName = reader.IsDBNull(4) ? null : reader.GetString(4),
                ExteriorColor = reader.IsDBNull(5) ? null : reader.GetString(5),
                Msrp = reader.GetDecimal(6),
                CurrentOdometer = reader.GetInt32(7),
                Status = reader.GetString(8)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<LocationListItemViewModel>> ReadLocationsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (50) loc.Id, b.Name, loc.LocationCode, loc.LocationName, loc.LocationType, loc.IsActive
            FROM Locations loc
            INNER JOIN Branches b ON b.Id = loc.BranchId
            ORDER BY b.Name, loc.LocationName;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<LocationListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new LocationListItemViewModel
            {
                Id = reader.GetInt32(0),
                BranchName = reader.GetString(1),
                LocationCode = reader.GetString(2),
                LocationName = reader.GetString(3),
                LocationType = reader.GetString(4),
                IsActive = reader.GetBoolean(5)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<FactoryOrderListItemViewModel>> ReadFactoryOrdersAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (30) fo.Id, fo.FactoryOrderNo, b.Name, fo.OrderDate, fo.ExpectedArrivalDate, fo.Status, COUNT(foi.Id), COALESCE(SUM(foi.Quantity), 0)
            FROM FactoryOrders fo
            INNER JOIN Branches b ON b.Id = fo.BranchId
            LEFT JOIN FactoryOrderItems foi ON foi.FactoryOrderId = fo.Id
            GROUP BY fo.Id, fo.FactoryOrderNo, b.Name, fo.OrderDate, fo.ExpectedArrivalDate, fo.Status
            ORDER BY MAX(fo.CreatedAt) DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<FactoryOrderListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new FactoryOrderListItemViewModel
            {
                Id = reader.GetInt32(0),
                FactoryOrderNo = reader.GetString(1),
                BranchName = reader.GetString(2),
                OrderDate = reader.GetDateTime(3),
                ExpectedArrivalDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Status = reader.GetString(5),
                ItemCount = reader.GetInt32(6),
                TotalQuantity = reader.GetInt32(7)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<PartListItemViewModel>> ReadPartsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (50) p.Id, p.PartCode, p.PartName, p.Unit, p.ListPrice, p.IsActive,
                   COALESCE(SUM(pi.QuantityOnHand), 0), COALESCE(MAX(pi.MinStockLevel), 0)
            FROM Parts p
            LEFT JOIN PartInventory pi ON pi.PartId = p.Id
            GROUP BY p.Id, p.PartCode, p.PartName, p.Unit, p.ListPrice, p.IsActive
            ORDER BY p.PartName;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<PartListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartListItemViewModel
            {
                Id = reader.GetInt32(0),
                PartCode = reader.GetString(1),
                PartName = reader.GetString(2),
                Unit = reader.GetString(3),
                ListPrice = reader.GetDecimal(4),
                IsActive = reader.GetBoolean(5),
                QuantityOnHand = reader.GetDecimal(6),
                MinStockLevel = reader.GetDecimal(7)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<PartPurchaseOrderListItemViewModel>> ReadPartPurchaseOrdersAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (30) po.Id, po.PurchaseOrderNo, b.Name, po.SupplierName, po.OrderDate, po.ExpectedArrivalDate, po.Status, COUNT(poi.Id), COALESCE(SUM(poi.OrderedQuantity), 0)
            FROM PartPurchaseOrders po
            INNER JOIN Branches b ON b.Id = po.BranchId
            LEFT JOIN PartPurchaseOrderItems poi ON poi.PartPurchaseOrderId = po.Id
            GROUP BY po.Id, po.PurchaseOrderNo, b.Name, po.SupplierName, po.OrderDate, po.ExpectedArrivalDate, po.Status
            ORDER BY MAX(po.CreatedAt) DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<PartPurchaseOrderListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartPurchaseOrderListItemViewModel
            {
                Id = reader.GetInt32(0),
                PurchaseOrderNo = reader.GetString(1),
                BranchName = reader.GetString(2),
                SupplierName = reader.GetString(3),
                OrderDate = reader.GetDateTime(4),
                ExpectedArrivalDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                Status = reader.GetString(6),
                ItemCount = reader.GetInt32(7),
                TotalQuantity = reader.GetDecimal(8)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<VehicleMovementListItemViewModel>> ReadVehicleMovementsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20) nv.Vin, vm.ModelName, fromLoc.LocationName, toLoc.LocationName, m.MovedAt, m.Reason
            FROM VehicleLocationMovements m
            INNER JOIN NewVehicles nv ON nv.Id = m.NewVehicleId
            INNER JOIN VehicleModels vm ON vm.Id = nv.VehicleModelId
            LEFT JOIN Locations fromLoc ON fromLoc.Id = m.FromLocationId
            INNER JOIN Locations toLoc ON toLoc.Id = m.ToLocationId
            ORDER BY m.MovedAt DESC, m.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<VehicleMovementListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new VehicleMovementListItemViewModel
            {
                Vin = reader.GetString(0),
                ModelName = reader.GetString(1),
                FromLocationName = reader.IsDBNull(2) ? null : reader.GetString(2),
                ToLocationName = reader.GetString(3),
                MovedAt = reader.GetDateTime(4),
                Reason = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<PartStockMovementListItemViewModel>> ReadPartStockMovementsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (20) p.PartCode, p.PartName, loc.LocationName, m.MovementType, m.QuantityDelta, m.CreatedAt, m.Note
            FROM PartStockMovements m
            INNER JOIN Parts p ON p.Id = m.PartId
            INNER JOIN Locations loc ON loc.Id = m.LocationId
            ORDER BY m.CreatedAt DESC, m.Id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<PartStockMovementListItemViewModel>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartStockMovementListItemViewModel
            {
                PartCode = reader.GetString(0),
                PartName = reader.GetString(1),
                LocationName = reader.GetString(2),
                MovementType = reader.GetString(3),
                QuantityDelta = reader.GetDecimal(4),
                CreatedAt = reader.GetDateTime(5),
                Note = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return items;
    }

    private static void AddMoney(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value;
    }

    private static void AddDecimal(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value;
    }

    private static object ToDbNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static bool IsDuplicateKey(SqlException? ex) => ex?.Number is 2601 or 2627;

    private static FriendlyOperationException CreateFriendlyException(string message, Exception? innerException = null)
    {
        if (innerException is SqlException { Number: 207 or 208 })
        {
            message = "Database schema chưa cập nhật. Hãy chạy `database/upgrade.sql` hoặc `database/setup.sql` rồi thử lại.";
        }

        return new FriendlyOperationException(message, innerException);
    }
}
