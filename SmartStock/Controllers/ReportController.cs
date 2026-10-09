using ClosedXML.Excel;
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
        // location: "" = all, "w" = all warehouses, "s" = all stores, "w-{id}" / "s-{id}" = a single location
        public async Task<IActionResult> Inventory(string? location, int? categoryId) =>
            View(await BuildInventoryReportAsync(location, categoryId));

        // GET: Report/InventoryPrint — standalone, print-formatted version of the Inventory report
        public async Task<IActionResult> InventoryPrint(string? location, int? categoryId) =>
            View(await BuildInventoryReportAsync(location, categoryId));

        // GET: Report/InventoryExport — Excel (.xlsx) version of the Inventory report, same filters
        public async Task<IActionResult> InventoryExport(string? location, int? categoryId)
        {
            var items = await BuildInventoryReportAsync(location, categoryId);
            string locationLabel = ViewBag.LocationLabel;
            string categoryLabel = ViewBag.CategoryLabel;
            string cur = ((string?)ViewBag.CurrencySymbol ?? "").Replace("\"", "");
            string money = $"\"{cur}\"#,##0.00;[Red]-\"{cur}\"#,##0.00";

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Inventory");

            // ── Title block ──
            ws.Cell(1, 1).Value = "Inventory Valuation Report";
            ws.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
            ws.Cell(2, 1).Value = $"Location: {locationLabel}   |   Category: {categoryLabel}   |   Exported: {DateTime.Now:dd MMM yyyy HH:mm}";
            ws.Cell(2, 1).Style.Font.SetFontColor(XLColor.Gray);

            // ── Header ──
            // A–D are raw keys (useful for lookups / re-import); LocationType is the stored enum value.
            string[] headers =
            {
                "Id", "ProductId", "LocationType", "LocationId",
                "Product", "SKU", "Category", "Location", "Type", "Qty", "Reorder Level", "Status",
                "Cost Price", "Selling Price", "Qty × Cost", "Qty × Selling", "P/L"
            };
            const int headerRow = 4;
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(headerRow, c + 1).Value = headers[c];
            var header = ws.Range(headerRow, 1, headerRow, headers.Length);
            header.Style.Font.SetBold().Font.SetFontColor(XLColor.White)
                  .Fill.SetBackgroundColor(XLColor.FromHtml("#1a1f2e"));

            // ── Rows (value columns are formulas, so edits in Excel recalculate) ──
            int r = headerRow + 1;
            foreach (var i in items)
            {
                ws.Cell(r, 1).Value = i.InventoryId;
                ws.Cell(r, 2).Value = i.ProductId;
                ws.Cell(r, 3).Value = (int)i.LocationType;
                ws.Cell(r, 4).Value = i.LocationId;
                ws.Cell(r, 5).Value = i.ProductName;
                ws.Cell(r, 6).Value = i.SKU;
                ws.Cell(r, 7).Value = i.CategoryName;
                ws.Cell(r, 8).Value = i.LocationName;
                ws.Cell(r, 9).Value = i.LocationType.ToString();
                ws.Cell(r, 10).Value = i.Quantity;
                ws.Cell(r, 11).Value = i.ReorderLevel;
                ws.Cell(r, 12).Value = i.Quantity == 0 ? "Out of Stock" : i.IsLowStock ? "Low Stock" : "OK";
                ws.Cell(r, 13).Value = i.CostPrice;
                ws.Cell(r, 14).Value = i.SellingPrice;
                ws.Cell(r, 15).FormulaA1 = $"J{r}*M{r}";
                ws.Cell(r, 16).FormulaA1 = $"J{r}*N{r}";
                ws.Cell(r, 17).FormulaA1 = $"P{r}-O{r}";
                r++;
            }
            int lastData = r - 1;

            // ── Totals ──
            if (items.Count > 0)
            {
                ws.Cell(r, 5).Value = "Total";
                foreach (var col in new[] { "J", "O", "P", "Q" })
                    ws.Cell($"{col}{r}").FormulaA1 = $"SUBTOTAL(9,{col}{headerRow + 1}:{col}{lastData})";
                var totals = ws.Range(r, 1, r, headers.Length);
                totals.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#ececec"))
                      .Border.SetTopBorder(XLBorderStyleValues.Thin);

                // Filter dropdowns on the data; SUBTOTAL makes the totals follow the filter.
                ws.Range(headerRow, 1, lastData, headers.Length).SetAutoFilter();
            }

            // ── Formats ──
            ws.Range(headerRow + 1, 10, r, 11).Style.NumberFormat.Format = "#,##0";
            ws.Range(headerRow + 1, 13, r, 17).Style.NumberFormat.Format = money;
            ws.SheetView.FreezeRows(headerRow);
            ws.Columns().AdjustToContents(headerRow, r);   // skip the title rows so column A (Id) stays narrow
            ws.Column(5).Width = Math.Min(ws.Column(5).Width, 50);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            var fileName = $"Inventory_{Slug(locationLabel)}_{Slug(categoryLabel)}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

            static string Slug(string s) =>
                string.Concat(s.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ')).Trim().Replace(' ', '-');
        }

        private async Task<List<InventoryViewModel>> BuildInventoryReportAsync(string? location, int? categoryId)
        {
            var warehouses = await _db.Warehouses.AsNoTracking().OrderBy(w => w.Name).ToListAsync();
            var stores = await _db.Stores.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
            var warehouseNames = warehouses.ToDictionary(w => w.Id, w => w.Name);
            var storeNames = stores.ToDictionary(s => s.Id, s => s.Name);

            location = location?.Trim().ToLowerInvariant() ?? "";
            LocationType? locationType = location.StartsWith("w") ? LocationType.Warehouse
                                       : location.StartsWith("s") ? LocationType.Store
                                       : null;
            int? locationId = location.Length > 2 && int.TryParse(location[2..], out var lid) ? lid : null;

            var query = _db.Inventories.AsNoTracking().Where(i => i.Product.IsActive);
            if (locationType.HasValue) query = query.Where(i => i.LocationType == locationType.Value);
            if (locationId.HasValue) query = query.Where(i => i.LocationId == locationId.Value);
            if (categoryId.HasValue) query = query.Where(i => i.Product.CategoryId == categoryId.Value);

            var inventory = await query
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
                    LastUpdated = i.LastUpdated,
                    CostPrice = i.Product.CostPrice,
                    SellingPrice = i.Product.Price
                })
                .ToListAsync();

            foreach (var item in inventory)
            {
                item.LocationName = item.LocationType == LocationType.Warehouse
                    ? warehouseNames.GetValueOrDefault(item.LocationId, "Warehouse")
                    : storeNames.GetValueOrDefault(item.LocationId, "Store");
            }

            ViewBag.Location = location;
            ViewBag.LocationLabel = (locationType, locationId) switch
            {
                (LocationType.Warehouse, int id) => warehouseNames.GetValueOrDefault(id, "Warehouse"),
                (LocationType.Store, int id)     => storeNames.GetValueOrDefault(id, "Store"),
                (LocationType.Warehouse, null)   => "All Warehouses",
                (LocationType.Store, null)       => "All Stores",
                _                                => "All Locations"
            };
            ViewBag.Warehouses = warehouses.Where(w => w.IsActive).Select(w => new SelectListItem(w.Name, $"w-{w.Id}"));
            ViewBag.Stores = stores.Where(s => s.IsActive).Select(s => new SelectListItem(s.Name, $"s-{s.Id}"));
            ViewBag.IsSingleLocation = locationId.HasValue;

            var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == categoryId));
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryLabel = categoryId.HasValue
                ? categories.FirstOrDefault(c => c.Id == categoryId.Value)?.Name ?? "Unknown Category"
                : "All Categories";

            return inventory;
        }

        // GET: Report/Transfers
        public async Task<IActionResult> Transfers(DateTime? from, DateTime? to, TransferStatus? status, int? warehouseId, int? storeId)
        {
            from ??= DateTime.UtcNow.AddDays(-30);
            to ??= DateTime.UtcNow;

            var query = _db.StockTransfers
                .Include(t => t.Warehouse)
                .Include(t => t.Store)
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Where(t => t.CreatedAt >= from && t.CreatedAt <= to.Value.AddDays(1))
                .AsQueryable();

            if (status.HasValue) query = query.Where(t => t.Status == status.Value);
            if (warehouseId.HasValue) query = query.Where(t => t.WarehouseId == warehouseId.Value);
            if (storeId.HasValue) query = query.Where(t => t.StoreId == storeId.Value);

            var transfers = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            var warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
            var stores = await _db.Stores.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To = to.Value.ToString("yyyy-MM-dd");
            ViewBag.Status = status;
            ViewBag.SelectedWarehouseId = warehouseId;
            ViewBag.SelectedStoreId = storeId;
            ViewBag.StatusOptions = Enum.GetValues<TransferStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()));
            ViewBag.Warehouses = warehouses.Select(w => new SelectListItem(w.Name, w.Id.ToString()));
            ViewBag.Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString()));
            ViewBag.TotalTransfers = transfers.Count;
            ViewBag.ReceivedTransfers = transfers.Count(t => t.Status == TransferStatus.Received);
            ViewBag.InTransit = transfers.Count(t => t.Status == TransferStatus.Dispatched);
            ViewBag.PendingTransfers = transfers.Count(t => t.Status == TransferStatus.Pending || t.Status == TransferStatus.Approved);

            return View(transfers);
        }

        // GET: Report/Purchases
        public async Task<IActionResult> Purchases(int? warehouseId, DateTime? from, DateTime? to, PurchaseStatus? status)
        {
            from ??= DateTime.UtcNow.AddDays(-30);
            to ??= DateTime.UtcNow;

            var query = _db.Purchases
                .Include(p => p.Warehouse)
                .Include(p => p.CreatedBy)
                .Include(p => p.Items).ThenInclude(i => i.Product)
                .Where(p => p.PurchaseDate >= from && p.PurchaseDate <= to.Value.AddDays(1))
                .AsQueryable();

            if (warehouseId.HasValue) query = query.Where(p => p.WarehouseId == warehouseId.Value);
            if (status.HasValue) query = query.Where(p => p.Status == status.Value);

            var purchases = await query.OrderByDescending(p => p.PurchaseDate).ToListAsync();
            var warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();

            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To = to.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedWarehouseId = warehouseId;
            ViewBag.Status = status;
            ViewBag.Warehouses = warehouses.Select(w => new SelectListItem(w.Name, w.Id.ToString()));
            ViewBag.StatusOptions = Enum.GetValues<PurchaseStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()));
            ViewBag.TotalOrders = purchases.Count;
            ViewBag.TotalAmount = purchases.Sum(p => p.TotalAmount);
            ViewBag.ReceivedOrders = purchases.Count(p => p.Status == PurchaseStatus.Received);
            ViewBag.PendingOrders = purchases.Count(p => p.Status == PurchaseStatus.Pending || p.Status == PurchaseStatus.Ordered);

            return View(purchases);
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
