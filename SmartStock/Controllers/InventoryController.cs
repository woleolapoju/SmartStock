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
    public class InventoryController : AppBaseController
    {
        private readonly IInventoryService _inventoryService;
        private readonly ApplicationDbContext _db;

        public InventoryController(IInventoryService inventoryService, ApplicationDbContext db)
        {
            _inventoryService = inventoryService;
            _db = db;
        }

        private const int PageSize = 20;

        private async Task<InventoryListViewModel> BuildListModelAsync(int? categoryId, string? search, string? sort, string? dir, int page)
        {
            var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            return new InventoryListViewModel
            {
                Page = Math.Max(1, page),
                PageSize = PageSize,
                CategoryId = categoryId,
                Search = search?.Trim(),
                Sort = string.IsNullOrWhiteSpace(sort) ? "name" : sort.ToLowerInvariant(),
                Dir = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc",
                Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == categoryId))
            };
        }

        // Send the user back to the exact list view (page, filter, sort) they posted from.
        private IActionResult RedirectBack(string? returnUrl, LocationType locationType, int locationId) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : locationType == LocationType.Warehouse
                    ? RedirectToAction(nameof(Warehouse), new { warehouseId = locationId })
                    : RedirectToAction(nameof(Store), new { storeId = locationId });

        public async Task<IActionResult> Warehouse(int? warehouseId, int? categoryId, string? search, string? sort, string? dir, int page = 1)
        {
            // Warehouse view is open to all roles (read-only for Staff/StoreManager)
            var warehouses = await _db.Warehouses.AsNoTracking().Where(w => w.IsActive).ToListAsync();
            var selectedId = warehouseId ?? warehouses.FirstOrDefault()?.Id ?? 0;

            ViewBag.Warehouses = warehouses.Select(w => new SelectListItem(w.Name, w.Id.ToString(), w.Id == selectedId));
            ViewBag.SelectedWarehouseId = selectedId;

            var model = await BuildListModelAsync(categoryId, search, sort, dir, page);
            if (selectedId > 0)
            {
                var (items, total) = await _inventoryService.GetWarehouseInventoryPagedAsync(selectedId, model.Page, PageSize, model.CategoryId, model.Sort, model.Dir == "desc", model.Search);
                model.Items = items;
                model.TotalCount = total;
            }

            return View(model);
        }

        public async Task<IActionResult> Store(int? storeId, int? categoryId, string? search, string? sort, string? dir, int page = 1)
        {
            bool locked = IsStoreRestricted();
            int? forcedId = locked ? GetUserStoreId() : null;

            // Locked users can only see their own store
            var stores = locked
                ? await _db.Stores.AsNoTracking().Where(s => s.Id == forcedId && s.IsActive).ToListAsync()
                : await _db.Stores.AsNoTracking().Where(s => s.IsActive).ToListAsync();

            var selectedId = forcedId ?? storeId ?? stores.FirstOrDefault()?.Id ?? 0;

            ViewBag.Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString(), s.Id == selectedId));
            ViewBag.SelectedStoreId = selectedId;
            ViewBag.IsStoreLocked = locked;

            var model = await BuildListModelAsync(categoryId, search, sort, dir, page);
            if (selectedId > 0)
            {
                var (items, total) = await _inventoryService.GetStoreInventoryPagedAsync(selectedId, model.Page, PageSize, model.CategoryId, model.Sort, model.Dir == "desc", model.Search);
                model.Items = items;
                model.TotalCount = total;
            }

            return View(model);
        }

        public async Task<IActionResult> LowStock(int page = 1)
        {
            // Restrict to assigned store for Staff/StoreManager
            int? restrictToStoreId = IsStoreRestricted() ? GetUserStoreId() : null;

            var model = new InventoryListViewModel { Page = Math.Max(1, page), PageSize = PageSize };
            var (items, total) = await _inventoryService.GetLowStockItemsPagedAsync(model.Page, PageSize, restrictToStoreId);
            model.Items = items;
            model.TotalCount = total;

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager,StoreManager")]
        public async Task<IActionResult> Adjust(StockAdjustmentViewModel model, string? returnUrl = null)
        {
            // StoreManager can only adjust their own store
            if (User.IsInRole("StoreManager") && model.LocationType == LocationType.Store)
            {
                var sid = GetUserStoreId();
                if (sid.HasValue && model.LocationId != sid.Value)
                {
                    TempData["Error"] = "You can only adjust inventory for your assigned store.";
                    return RedirectToAction(nameof(Store), new { storeId = sid });
                }
            }

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

            return RedirectBack(returnUrl, model.LocationType, model.LocationId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager,StoreManager")]
        public async Task<IActionResult> UpdateReorderLevel(int inventoryId, int reorderLevel, LocationType locationType, int locationId, string? returnUrl = null)
        {
            var result = await _inventoryService.UpdateReorderLevelAsync(inventoryId, reorderLevel);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();

            return RedirectBack(returnUrl, locationType, locationId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> UpdatePrices(int productId, decimal costPrice, decimal sellingPrice, LocationType locationType, int locationId, string? returnUrl = null)
        {
            var result = await _inventoryService.UpdateProductPricesAsync(productId, costPrice, sellingPrice);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();

            return RedirectBack(returnUrl, locationType, locationId);
        }
    }
}
