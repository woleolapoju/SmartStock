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
            var warehouse = await _db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == warehouseId);
            return await _db.Inventories
                .AsNoTracking()
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
            var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == storeId);
            return await _db.Inventories
                .AsNoTracking()
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
            var warehouses = await _db.Warehouses.AsNoTracking().ToDictionaryAsync(w => w.Id, w => w.Name);
            var stores = await _db.Stores.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Name);

            return await _db.Inventories
                .AsNoTracking()
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

        public async Task<(IEnumerable<InventoryViewModel> Items, int TotalCount)> GetWarehouseInventoryPagedAsync(
            int warehouseId, int page, int pageSize, int? categoryId = null, string? sort = null, bool desc = false, string? search = null)
        {
            var warehouseName = await _db.Warehouses.AsNoTracking()
                .Where(w => w.Id == warehouseId)
                .Select(w => w.Name)
                .FirstOrDefaultAsync() ?? $"Warehouse {warehouseId}";

            return await GetLocationInventoryPagedAsync(LocationType.Warehouse, warehouseId, warehouseName, page, pageSize, categoryId, sort, desc, search);
        }

        public async Task<(IEnumerable<InventoryViewModel> Items, int TotalCount)> GetStoreInventoryPagedAsync(
            int storeId, int page, int pageSize, int? categoryId = null, string? sort = null, bool desc = false, string? search = null)
        {
            var storeName = await _db.Stores.AsNoTracking()
                .Where(s => s.Id == storeId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync() ?? $"Store {storeId}";

            return await GetLocationInventoryPagedAsync(LocationType.Store, storeId, storeName, page, pageSize, categoryId, sort, desc, search);
        }

        private async Task<(IEnumerable<InventoryViewModel> Items, int TotalCount)> GetLocationInventoryPagedAsync(
            LocationType locationType, int locationId, string locationName,
            int page, int pageSize, int? categoryId, string? sort, bool desc, string? search)
        {
            var query = _db.Inventories
                .AsNoTracking()
                .Where(i => i.LocationType == locationType && i.LocationId == locationId && i.Product.IsActive);

            if (categoryId.HasValue)
                query = query.Where(i => i.Product.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(i =>
                    i.Product.Name.Contains(search) ||
                    i.Product.SKU.Contains(search) ||
                    (i.Product.Barcode != null && i.Product.Barcode.Contains(search)));
            }

            var totalCount = await query.CountAsync();

            // Product name is the tiebreaker so paging stays stable across equal sort keys.
            IOrderedQueryable<Inventory> ordered = (sort?.ToLowerInvariant()) switch
            {
                "sku"      => desc ? query.OrderByDescending(i => i.Product.SKU)           : query.OrderBy(i => i.Product.SKU),
                "category" => desc ? query.OrderByDescending(i => i.Product.Category.Name) : query.OrderBy(i => i.Product.Category.Name),
                "qty"      => desc ? query.OrderByDescending(i => i.Quantity)              : query.OrderBy(i => i.Quantity),
                "reorder"  => desc ? query.OrderByDescending(i => i.ReorderLevel)          : query.OrderBy(i => i.ReorderLevel),
                "cost"     => desc ? query.OrderByDescending(i => i.Product.CostPrice)     : query.OrderBy(i => i.Product.CostPrice),
                "price"    => desc ? query.OrderByDescending(i => i.Product.Price)         : query.OrderBy(i => i.Product.Price),
                "status"   => desc ? query.OrderByDescending(i => i.Quantity <= i.ReorderLevel) : query.OrderBy(i => i.Quantity <= i.ReorderLevel),
                "updated"  => desc ? query.OrderByDescending(i => i.LastUpdated)           : query.OrderBy(i => i.LastUpdated),
                _          => desc ? query.OrderByDescending(i => i.Product.Name)          : query.OrderBy(i => i.Product.Name),
            };

            var items = await ordered
                .ThenBy(i => i.Product.Name)
                .Skip(Math.Max(0, page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new InventoryViewModel
                {
                    InventoryId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    CategoryName = i.Product.Category.Name,
                    LocationType = i.LocationType,
                    LocationId = i.LocationId,
                    LocationName = locationName,
                    Quantity = i.Quantity,
                    ReorderLevel = i.ReorderLevel,
                    LastUpdated = i.LastUpdated,
                    CostPrice = i.Product.CostPrice,
                    SellingPrice = i.Product.Price
                })
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<InventoryViewModel> Items, int TotalCount)> GetLowStockItemsPagedAsync(int page, int pageSize, int? restrictToStoreId = null)
        {
            var query = _db.Inventories
                .AsNoTracking()
                .Where(i => i.Quantity <= i.ReorderLevel && i.Product.IsActive);

            if (restrictToStoreId.HasValue)
                query = query.Where(i => i.LocationType == LocationType.Store && i.LocationId == restrictToStoreId.Value);

            var totalCount = await query.CountAsync();

            var pagedRaw = await query
                .OrderBy(i => i.Quantity)
                .Skip(Math.Max(0, page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.Product.Name,
                    i.Product.SKU,
                    CategoryName = i.Product.Category.Name,
                    i.LocationType,
                    i.LocationId,
                    i.Quantity,
                    i.ReorderLevel,
                    i.LastUpdated
                })
                .ToListAsync();

            // Resolve location names only for the locations present on this page,
            // instead of loading every warehouse/store up front.
            var warehouseIds = pagedRaw.Where(x => x.LocationType == LocationType.Warehouse).Select(x => x.LocationId).Distinct().ToList();
            var storeIds = pagedRaw.Where(x => x.LocationType == LocationType.Store).Select(x => x.LocationId).Distinct().ToList();

            var warehouseNames = await _db.Warehouses.AsNoTracking()
                .Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Name);
            var storeNames = await _db.Stores.AsNoTracking()
                .Where(s => storeIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name);

            var items = pagedRaw.Select(x => new InventoryViewModel
            {
                InventoryId = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Name,
                SKU = x.SKU,
                CategoryName = x.CategoryName,
                LocationType = x.LocationType,
                LocationId = x.LocationId,
                LocationName = x.LocationType == LocationType.Warehouse
                    ? warehouseNames.GetValueOrDefault(x.LocationId, "Warehouse")
                    : storeNames.GetValueOrDefault(x.LocationId, "Store"),
                Quantity = x.Quantity,
                ReorderLevel = x.ReorderLevel,
                LastUpdated = x.LastUpdated
            }).ToList();

            return (items, totalCount);
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

        // Prices live on the Product, so this applies to every warehouse and store.
        public async Task<ServiceResult> UpdateProductPricesAsync(int productId, decimal costPrice, decimal sellingPrice)
        {
            if (costPrice < 0 || sellingPrice < 0)
                return ServiceResult.Fail("Prices cannot be negative.");

            var product = await _db.Products.FindAsync(productId);
            if (product == null)
                return ServiceResult.Fail("Product not found.");

            product.CostPrice = Math.Round(costPrice, 2);
            product.Price = Math.Round(sellingPrice, 2);
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var msg = $"Prices updated for {product.Name}.";
            if (product.Price < product.CostPrice)
                msg += " Note: selling price is below cost price.";
            return ServiceResult.Ok(msg);
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
