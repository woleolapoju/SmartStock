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
                .Where(i => i.LocationType == LocationType.Warehouse && i.LocationId == warehouseId)
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
                .Where(i => i.LocationType == LocationType.Store && i.LocationId == storeId)
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
            // Load warehouses and stores for name resolution
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

        public async Task<ServiceResult> AdjustStockAsync(StockAdjustmentViewModel model)
        {
            var inventory = await GetInventoryAsync(model.ProductId, model.LocationType, model.LocationId);
            if (inventory == null)
                return ServiceResult.Fail("Inventory record not found for this product/location.");

            var newQty = inventory.Quantity + model.QuantityChange;
            if (newQty < 0)
                return ServiceResult.Fail($"Insufficient stock. Current: {inventory.Quantity}, Requested: {Math.Abs(model.QuantityChange)}");

            inventory.Quantity = newQty;
            inventory.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Stock adjusted: Product {ProductId} at {Type}/{LocationId} by {Change}",
                model.ProductId, model.LocationType, model.LocationId, model.QuantityChange);

            return ServiceResult.Ok($"Stock adjusted. New quantity: {newQty}");
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
    }
}
