using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(ApplicationDbContext db, ILogger<InventoryService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<InventoryViewModel>> GetWarehouseInventoryAsync(int warehouseId)
        {
            var warehouse = await _db.Warehouses.FindAsync(warehouseId);
            return await _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.LocationType == LocationType.Warehouse && i.LocationId == warehouseId && i.Product.IsActive)
                .Select(i => new InventoryViewModel
                {
                    InventoryId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    CategoryName = i.Product.Category.Name,
                    LocationType = i.LocationType,
                    LocationId = i.LocationId,
                    LocationName = warehouse != null ? warehouse.Name : "Warehouse " + warehouseId,
                    Quantity = i.Quantity,
                    ReorderLevel = i.ReorderLevel,
                    LastUpdated = i.LastUpdated
                })
                .OrderBy(i => i.ProductName)
                .ToListAsync();
        }

        public async Task<IEnumerable<InventoryViewModel>> GetStoreInventoryAsync(int storeId)
        {
            var store = await _db.Stores.FindAsync(storeId);
            return await _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.LocationType == LocationType.Store && i.LocationId == storeId && i.Product.IsActive)
                .Select(i => new InventoryViewModel
                {
                    InventoryId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    CategoryName = i.Product.Category.Name,
                    LocationType = i.LocationType,
                    LocationId = i.LocationId,
                    LocationName = store != null ? store.Name : "Store " + storeId,
                    Quantity = i.Quantity,
                    ReorderLevel = i.ReorderLevel,
                    LastUpdated = i.LastUpdated
                })
                .OrderBy(i => i.ProductName)
                .ToListAsync();
        }

        public async Task<IEnumerable<InventoryViewModel>> GetLowStockItemsAsync()
        {
            var warehouses = await _db.Warehouses.ToDictionaryAsync(w => w.Id, w => w.Name);
            var stores = await _db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name);

            return await _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.Quantity <= i.ReorderLevel && i.Product.IsActive)
                .Select(i => new InventoryViewModel
                {
                    InventoryId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    CategoryName = i.Product.Category.Name,
                    LocationType = i.LocationType,
                    LocationId = i.LocationId,
                    LocationName = i.LocationType == LocationType.Warehouse
                        ? (warehouses.ContainsKey(i.LocationId) ? warehouses[i.LocationId] : "Warehouse")
                        : (stores.ContainsKey(i.LocationId) ? stores[i.LocationId] : "Store"),
                    Quantity = i.Quantity,
                    ReorderLevel = i.ReorderLevel,
                    LastUpdated = i.LastUpdated
                })
                .OrderBy(i => i.Quantity)
                .ToListAsync();
        }

        public async Task<Inventory?> GetInventoryAsync(int productId, LocationType locationType, int locationId) =>
            await _db.Inventories.FirstOrDefaultAsync(i =>
                i.ProductId == productId &&
                i.LocationType == locationType &&
                i.LocationId == locationId);

        public async Task<ServiceResult> AdjustStockAsync(StockAdjustmentViewModel model, string? userId = null)
        {
            var inventory = await GetInventoryAsync(model.ProductId, model.LocationType, model.LocationId);
            if (inventory == null)
                return ServiceResult.Fail("Inventory record not found for this product/location.");

            var newQty = inventory.Quantity + model.QuantityChange;
            if (newQty < 0)
                return ServiceResult.Fail($"Insufficient stock. Current: {inventory.Quantity}, Requested deduction: {Math.Abs(model.QuantityChange)}");

            // Log the adjustment
            _db.StockAdjustmentLogs.Add(new StockAdjustmentLog
            {
                ProductId = model.ProductId,
                LocationType = model.LocationType,
                LocationId = model.LocationId,
                QuantityBefore = inventory.Quantity,
                QuantityChange = model.QuantityChange,
                QuantityAfter = newQty,
                Reason = model.Reason,
                AdjustedByUserId = userId,
                AdjustedAt = DateTime.UtcNow
            });

            inventory.Quantity = newQty;
            inventory.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Stock adjusted: Product {ProductId} at {Type}/{LocationId} by {Change}",
                model.ProductId, model.LocationType, model.LocationId, model.QuantityChange);

            return ServiceResult.Ok($"Stock adjusted. New quantity: {newQty}");
        }

        public async Task<ServiceResult> UpdateReorderLevelAsync(int inventoryId, int reorderLevel)
        {
            if (reorderLevel < 0)
                return ServiceResult.Fail("Reorder level cannot be negative.");

            var inventory = await _db.Inventories.FindAsync(inventoryId);
            if (inventory == null)
                return ServiceResult.Fail("Inventory record not found.");

            inventory.ReorderLevel = reorderLevel;
            inventory.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Reorder level updated.");
        }

        public async Task<ServiceResult> EnsureInventoryRowAsync(int productId, LocationType locationType, int locationId, int reorderLevel = 10)
        {
            var existing = await GetInventoryAsync(productId, locationType, locationId);
            if (existing != null) return ServiceResult.Ok();

            _db.Inventories.Add(new Inventory
            {
                ProductId = productId,
                LocationType = locationType,
                LocationId = locationId,
                Quantity = 0,
                ReorderLevel = reorderLevel,
                LastUpdated = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<IEnumerable<StockAdjustmentLog>> GetAdjustmentLogsAsync(
            DateTime? from, DateTime? to, int? productId, LocationType? locationType, int? locationId)
        {
            var query = _db.StockAdjustmentLogs
                .Include(l => l.Product).ThenInclude(p => p.Category)
                .Include(l => l.AdjustedBy)
                .AsQueryable();

            if (from.HasValue) query = query.Where(l => l.AdjustedAt >= from.Value);
            if (to.HasValue) query = query.Where(l => l.AdjustedAt <= to.Value.AddDays(1));
            if (productId.HasValue) query = query.Where(l => l.ProductId == productId.Value);
            if (locationType.HasValue) query = query.Where(l => l.LocationType == locationType.Value);
            if (locationId.HasValue) query = query.Where(l => l.LocationId == locationId.Value);

            return await query.OrderByDescending(l => l.AdjustedAt).ToListAsync();
        }
    }
}
