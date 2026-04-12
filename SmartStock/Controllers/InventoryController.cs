using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ApplicationDbContext _db;

        public InventoryController(IInventoryService inventoryService, ApplicationDbContext db)
        {
            _inventoryService = inventoryService;
            _db = db;
        }

        public async Task<IActionResult> Warehouse(int? warehouseId)
        {
            var warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            var selectedId = warehouseId ?? warehouses.FirstOrDefault()?.Id ?? 0;

            ViewBag.Warehouses = warehouses.Select(w => new SelectListItem(w.Name, w.Id.ToString(), w.Id == selectedId));
            ViewBag.SelectedWarehouseId = selectedId;

            var inventory = selectedId > 0
                ? await _inventoryService.GetWarehouseInventoryAsync(selectedId)
                : Enumerable.Empty<InventoryViewModel>();

            return View(inventory);
        }

        public async Task<IActionResult> Store(int? storeId)
        {
            var stores = await _db.Stores.Where(s => s.IsActive).ToListAsync();
            var selectedId = storeId ?? stores.FirstOrDefault()?.Id ?? 0;

            ViewBag.Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString(), s.Id == selectedId));
            ViewBag.SelectedStoreId = selectedId;

            var inventory = selectedId > 0
                ? await _inventoryService.GetStoreInventoryAsync(selectedId)
                : Enumerable.Empty<InventoryViewModel>();

            return View(inventory);
        }

        public async Task<IActionResult> LowStock()
        {
            var items = await _inventoryService.GetLowStockItemsAsync();
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager,StoreManager")]
        public async Task<IActionResult> Adjust(StockAdjustmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid adjustment data.";
            }
            else
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var result = await _inventoryService.AdjustStockAsync(model, userId);
                TempData[result.Success ? "Success" : "Error"] =
                    result.Success ? result.Message : result.Errors.FirstOrDefault();
            }

            if (model.LocationType == LocationType.Warehouse)
                return RedirectToAction(nameof(Warehouse), new { warehouseId = model.LocationId });
            else
                return RedirectToAction(nameof(Store), new { storeId = model.LocationId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager,StoreManager")]
        public async Task<IActionResult> UpdateReorderLevel(int inventoryId, int reorderLevel, LocationType locationType, int locationId)
        {
            var result = await _inventoryService.UpdateReorderLevelAsync(inventoryId, reorderLevel);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();

            if (locationType == LocationType.Warehouse)
                return RedirectToAction(nameof(Warehouse), new { warehouseId = locationId });
            else
                return RedirectToAction(nameof(Store), new { storeId = locationId });
        }
    }
}
