using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Services
{
    public class TransferService : ITransferService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TransferService> _logger;

        public TransferService(ApplicationDbContext db, ILogger<TransferService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<StockTransfer>> GetAllAsync() =>
            await _db.StockTransfers
                .Include(t => t.Warehouse)
                .Include(t => t.Store)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

        public async Task<StockTransfer?> GetByIdAsync(int id) =>
            await _db.StockTransfers
                .Include(t => t.Warehouse)
                .Include(t => t.Store)
                .Include(t => t.CreatedBy)
                .Include(t => t.ApprovedBy)
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(t => t.Id == id);

        public async Task<ServiceResult> CreateAsync(StockTransferViewModel model, string userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Validate warehouse stock availability
                foreach (var item in model.Items)
                {
                    var stock = await _db.Inventories.FirstOrDefaultAsync(i =>
                        i.ProductId == item.ProductId &&
                        i.LocationType == LocationType.Warehouse &&
                        i.LocationId == model.WarehouseId);

                    if (stock == null || stock.Quantity < item.Quantity)
                    {
                        var product = await _db.Products.FindAsync(item.ProductId);
                        return ServiceResult.Fail($"Insufficient warehouse stock for '{product?.Name ?? "Unknown"}'. Available: {stock?.Quantity ?? 0}");
                    }
                }

                var transfer = new StockTransfer
                {
                    ReferenceNumber = GenerateRef("TRF"),
                    WarehouseId = model.WarehouseId,
                    StoreId = model.StoreId,
                    Notes = model.Notes,
                    Status = TransferStatus.Pending,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Items = model.Items.Select(i => new StockTransferItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity
                    }).ToList()
                };

                _db.StockTransfers.Add(transfer);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Transfer {Ref} created by {User}", transfer.ReferenceNumber, userId);
                return ServiceResult.Ok($"Transfer {transfer.ReferenceNumber} created.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error creating transfer");
                return ServiceResult.Fail("An error occurred creating the transfer.");
            }
        }

        public async Task<ServiceResult> ApproveAsync(int id, string userId)
        {
            var transfer = await _db.StockTransfers.FindAsync(id);
            if (transfer == null) return ServiceResult.Fail("Transfer not found.");
            if (transfer.Status != TransferStatus.Pending)
                return ServiceResult.Fail("Only pending transfers can be approved.");

            transfer.Status = TransferStatus.Approved;
            transfer.ApprovedByUserId = userId;
            transfer.ApprovedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Transfer approved.");
        }

        public async Task<ServiceResult> DispatchAsync(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _db.StockTransfers
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transfer == null) return ServiceResult.Fail("Transfer not found.");
                if (transfer.Status != TransferStatus.Approved)
                    return ServiceResult.Fail("Only approved transfers can be dispatched.");

                // Deduct from warehouse
                foreach (var item in transfer.Items)
                {
                    var warehouseStock = await _db.Inventories.FirstOrDefaultAsync(i =>
                        i.ProductId == item.ProductId &&
                        i.LocationType == LocationType.Warehouse &&
                        i.LocationId == transfer.WarehouseId);

                    if (warehouseStock == null || warehouseStock.Quantity < item.Quantity)
                    {
                        await tx.RollbackAsync();
                        return ServiceResult.Fail($"Insufficient warehouse stock for product ID {item.ProductId}.");
                    }

                    warehouseStock.Quantity -= item.Quantity;
                    warehouseStock.LastUpdated = DateTime.UtcNow;
                }

                transfer.Status = TransferStatus.Dispatched;
                transfer.DispatchedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult.Ok("Transfer dispatched. Warehouse stock deducted.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error dispatching transfer {Id}", id);
                return ServiceResult.Fail("An error occurred during dispatch.");
            }
        }

        public async Task<ServiceResult> ReceiveAsync(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _db.StockTransfers
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (transfer == null) return ServiceResult.Fail("Transfer not found.");
                if (transfer.Status != TransferStatus.Dispatched)
                    return ServiceResult.Fail("Only dispatched transfers can be received.");

                // Add to store
                foreach (var item in transfer.Items)
                {
                    var storeStock = await _db.Inventories.FirstOrDefaultAsync(i =>
                        i.ProductId == item.ProductId &&
                        i.LocationType == LocationType.Store &&
                        i.LocationId == transfer.StoreId);

                    if (storeStock == null)
                    {
                        storeStock = new Inventory
                        {
                            ProductId = item.ProductId,
                            LocationType = LocationType.Store,
                            LocationId = transfer.StoreId,
                            Quantity = 0,
                            ReorderLevel = 10
                        };
                        _db.Inventories.Add(storeStock);
                    }

                    storeStock.Quantity += item.Quantity;
                    storeStock.LastUpdated = DateTime.UtcNow;
                }

                transfer.Status = TransferStatus.Received;
                transfer.ReceivedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ServiceResult.Ok("Transfer received. Store stock updated.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error receiving transfer {Id}", id);
                return ServiceResult.Fail("An error occurred during receipt.");
            }
        }

        public async Task<ServiceResult> CancelAsync(int id)
        {
            var transfer = await _db.StockTransfers.FindAsync(id);
            if (transfer == null) return ServiceResult.Fail("Transfer not found.");
            if (transfer.Status is TransferStatus.Dispatched or TransferStatus.Received)
                return ServiceResult.Fail("Cannot cancel a dispatched or received transfer.");

            transfer.Status = TransferStatus.Cancelled;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Transfer cancelled.");
        }

        private static string GenerateRef(string prefix) =>
            $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }
}
