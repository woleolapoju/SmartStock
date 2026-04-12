using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<InventoryViewModel>> GetWarehouseInventoryAsync(int warehouseId);
        Task<IEnumerable<InventoryViewModel>> GetStoreInventoryAsync(int storeId);
        Task<IEnumerable<InventoryViewModel>> GetLowStockItemsAsync();
        Task<Inventory?> GetInventoryAsync(int productId, LocationType locationType, int locationId);
        Task<ServiceResult> AdjustStockAsync(StockAdjustmentViewModel model, string? userId = null);
        Task<ServiceResult> UpdateReorderLevelAsync(int inventoryId, int reorderLevel);
        Task<ServiceResult> EnsureInventoryRowAsync(int productId, LocationType locationType, int locationId, int reorderLevel = 10);
        Task<IEnumerable<StockAdjustmentLog>> GetAdjustmentLogsAsync(DateTime? from, DateTime? to, int? productId, LocationType? locationType, int? locationId);
    }
}
