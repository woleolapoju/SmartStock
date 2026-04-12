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
    [Authorize(Roles = "Admin,WarehouseManager,StoreManager")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventoryService;

        public ReportController(ApplicationDbContext db, IInventoryService inventoryService)
        {
            _db = db;
            _inventoryService = inventoryService;
        }

        // GET: Report/Sales
        public async Task<IActionResult> Sales(int? storeId, DateTime? from, DateTime? to)
        {
            from ??= DateTime.UtcNow.AddDays(-30);
            to ??= DateTime.UtcNow;

            var query = _db.Sales
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .Where(s => s.SaleDate >= from && s.SaleDate <= to.Value.AddDays(1) && s.Status == SaleStatus.Completed)
                .AsQueryable();

            if (storeId.HasValue) query = query.Where(s => s.StoreId == storeId.Value);

            var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();
            var stores = await _db.Stores.Where(s => s.IsActive).ToListAsync();

            ViewBag.Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString()));
            ViewBag.SelectedStoreId = storeId;
            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To = to.Value.ToString("yyyy-MM-dd");
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalTransactions = sales.Count;

            return View(sales);
        }

        // GET: Report/Inventory
        public async Task<IActionResult> Inventory()
        {
            var warehouses = await _db.Warehouses.ToDictionaryAsync(w => w.Id, w => w.Name);
            var stores = await _db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name);

            var inventory = await _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.Product.IsActive)
                .OrderBy(i => i.Product.Category.Name).ThenBy(i => i.Product.Name)
                .Select(i => new InventoryViewModel
                {
                    InventoryId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    CategoryName = i.Product.Category.Name,
                    LocationType = i.LocationType,
                    LocationId = i.LocationId,
                    Quantity = i.Quantity,
                    ReorderLevel = i.ReorderLevel,
                    LastUpdated = i.LastUpdated
                })
                .ToListAsync();

            foreach (var item in inventory)
            {
                item.LocationName = item.LocationType == LocationType.Warehouse
                    ? (warehouses.TryGetValue(item.LocationId, out var wn) ? wn : "Warehouse")
                    : (stores.TryGetValue(item.LocationId, out var sn) ? sn : "Store");
            }

            return View(inventory);
        }

        // GET: Report/Transfers
        public async Task<IActionResult> Transfers(DateTime? from, DateTime? to, TransferStatus? status)
        {
            from ??= DateTime.UtcNow.AddDays(-30);
            to ??= DateTime.UtcNow;

            var query = _db.StockTransfers
                .Include(t => t.Warehouse)
                .Include(t => t.Store)
                .Include(t => t.Items)
                .Where(t => t.CreatedAt >= from && t.CreatedAt <= to.Value.AddDays(1))
                .AsQueryable();

            if (status.HasValue) query = query.Where(t => t.Status == status.Value);

            var transfers = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To = to.Value.ToString("yyyy-MM-dd");
            ViewBag.Status = status;
            ViewBag.StatusOptions = Enum.GetValues<TransferStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()));

            return View(transfers);
        }

        // GET: Report/AdjustmentLog
        public async Task<IActionResult> AdjustmentLog(DateTime? from, DateTime? to, int? productId, LocationType? locationType, int? locationId)
        {
            from ??= DateTime.UtcNow.AddDays(-30);
            to ??= DateTime.UtcNow;

            var logs = await _inventoryService.GetAdjustmentLogsAsync(from, to.Value.AddDays(1), productId, locationType, locationId);

            var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            var warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
            var stores = await _db.Stores.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To = to.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedProductId = productId;
            ViewBag.SelectedLocationType = locationType;
            ViewBag.SelectedLocationId = locationId;
            ViewBag.Products = products.Select(p => new SelectListItem($"{p.Name} ({p.SKU})", p.Id.ToString()));
            ViewBag.LocationTypeOptions = new[]
            {
                new SelectListItem("Warehouse", "1"),
                new SelectListItem("Store", "2")
            };
            ViewBag.Warehouses = warehouses.Select(w => new SelectListItem(w.Name, w.Id.ToString()));
            ViewBag.Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString()));

            // Build location name lookup
            var warehouseNames = warehouses.ToDictionary(w => w.Id, w => w.Name);
            var storeNames = stores.ToDictionary(s => s.Id, s => s.Name);
            ViewBag.WarehouseNames = warehouseNames;
            ViewBag.StoreNames = storeNames;

            return View(logs);
        }
    }
}
