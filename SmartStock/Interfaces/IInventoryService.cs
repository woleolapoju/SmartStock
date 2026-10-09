using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<InventoryViewModel>> GetWarehouseInventoryAsync(int warehouseId);
        Task<IEnumerable<InventoryViewModel>> GetStoreInventoryAsync(int storeId);
        Task<IEnumerable<InventoryViewModel>> GetLowStockItemsAsync();

        // Paginated variants backing the MVC list pages — page/filter at the database
        // level instead of loading every row into memory with full change tracking.
        Task<(IEnumerable<InventoryViewModel> Items, int TotalCount)> GetWarehouseInventoryPagedAsync(int warehouseId, int page, int pageSize, int? categoryId = null, string? sort = null, bool desc = false, string? search = null);
        Task<(IEnumerable<InventoryViewModel> Items, int TotalCount)> GetStoreInventoryPagedAsync(int storeId, int page, int pageSize, int? categoryId = null, string? sort = null, bool desc = false, string? search = null);
        Task<(IEnumerable<InventoryViewModel> Items, int TotalCount)> GetLowStockItemsPagedAsync(int page, int pageSize, int? restrictToStoreId = null);
        Task<Inventory?> GetInventoryAsync(int productId, LocationType locationType, int locationId);
        Task<ServiceResult> AdjustStockAsync(StockAdjustmentViewModel model, string? userId = null);
        Task<ServiceResult> UpdateReorderLevelAsync(int inventoryId, int reorderLevel);
        Task<ServiceResult> UpdateProductPricesAsync(int productId, decimal costPrice, decimal sellingPrice);
        Task<ServiceResult> EnsureInventoryRowAsync(int productId, LocationType locationType, int locationId, int reorderLevel = 10);
        Task<IEnumerable<StockAdjustmentLog>> GetAdjustmentLogsAsync(DateTime? from, DateTime? to, int? productId, LocationType? locationType, int? locationId);
    }
}
