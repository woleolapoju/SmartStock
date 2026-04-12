using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;

        public DashboardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(int? storeId = null)
        {
            var today = DateTime.UtcNow.Date;
            bool scoped = storeId.HasValue;

            // ── KPIs ────────────────────────────────────────────────────────────
            var totalProducts = await _db.Products.CountAsync(p => p.IsActive);
            var totalWarehouses = await _db.Warehouses.CountAsync(w => w.IsActive);

            var totalStores = scoped ? 1 : await _db.Stores.CountAsync(s => s.IsActive);

            var stockQuery = _db.Inventories.AsQueryable();
            if (scoped)
                stockQuery = stockQuery.Where(i => i.LocationType == LocationType.Store && i.LocationId == storeId);
            var totalStock = await stockQuery.SumAsync(i => (long)i.Quantity);

            var lowStockQuery = _db.Inventories.Where(i => i.Quantity <= i.ReorderLevel && i.Product.IsActive);
            if (scoped)
                lowStockQuery = lowStockQuery.Where(i => i.LocationType == LocationType.Store && i.LocationId == storeId);
            var lowStockCount = await lowStockQuery.CountAsync();

            var salesTodayQuery = _db.Sales.Where(s => s.SaleDate >= today && s.Status == SaleStatus.Completed);
            if (scoped) salesTodayQuery = salesTodayQuery.Where(s => s.StoreId == storeId);
            var todaySales = await salesTodayQuery
                .GroupBy(_ => 1)
                .Select(g => new { Amount = g.Sum(s => s.TotalAmount), Count = g.Count() })
                .FirstOrDefaultAsync();

            var transfersQuery = _db.StockTransfers.Where(t =>
                t.Status == TransferStatus.Pending || t.Status == TransferStatus.Approved);
            if (scoped) transfersQuery = transfersQuery.Where(t => t.StoreId == storeId);
            var pendingTransfers = await transfersQuery.CountAsync();

            // ── Monthly Sales Chart (last 6 months) ─────────────────────────────
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var monthlySalesQuery = _db.Sales.Where(s =>
                s.SaleDate >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1)
                && s.Status == SaleStatus.Completed);
            if (scoped) monthlySalesQuery = monthlySalesQuery.Where(s => s.StoreId == storeId);
            var monthlySales = await monthlySalesQuery
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(s => s.TotalAmount) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // ── Category Stock Chart ─────────────────────────────────────────────
            var categoryStockQuery = _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.Product.IsActive);
            if (scoped)
                categoryStockQuery = categoryStockQuery.Where(i =>
                    i.LocationType == LocationType.Store && i.LocationId == storeId);
            var categoryStock = await categoryStockQuery
                .GroupBy(i => i.Product.Category.Name)
                .Select(g => new { Category = g.Key, Total = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Total)
                .Take(6)
                .ToListAsync();

            // ── Recent Sales ─────────────────────────────────────────────────────
            var recentSalesQuery = _db.Sales.Include(s => s.Store).Include(s => s.Cashier)
                .OrderByDescending(s => s.SaleDate);
            List<Sale> recentSales;
            if (scoped)
                recentSales = await recentSalesQuery.Where(s => s.StoreId == storeId).Take(5).ToListAsync();
            else
                recentSales = await recentSalesQuery.Take(5).ToListAsync();

            // ── Recent Transfers ─────────────────────────────────────────────────
            var recentTransfersQuery = _db.StockTransfers
                .Include(t => t.Warehouse).Include(t => t.Store)
                .OrderByDescending(t => t.CreatedAt);
            List<StockTransfer> recentTransfers;
            if (scoped)
                recentTransfers = await recentTransfersQuery.Where(t => t.StoreId == storeId).Take(5).ToListAsync();
            else
                recentTransfers = await recentTransfersQuery.Take(5).ToListAsync();

            // ── Low Stock Items ──────────────────────────────────────────────────
            var warehouses = await _db.Warehouses.ToDictionaryAsync(w => w.Id, w => w.Name);
            var stores     = await _db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name);

            var lowItemsQuery = _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.Quantity <= i.ReorderLevel && i.Product.IsActive);
            if (scoped)
                lowItemsQuery = lowItemsQuery.Where(i =>
                    i.LocationType == LocationType.Store && i.LocationId == storeId);

            var lowStockItems = await lowItemsQuery
                .OrderBy(i => i.Quantity).Take(10)
                .Select(i => new InventoryViewModel
                {
                    InventoryId  = i.Id,
                    ProductId    = i.ProductId,
                    ProductName  = i.Product.Name,
                    SKU          = i.Product.SKU,
                    CategoryName = i.Product.Category.Name,
                    LocationType = i.LocationType,
                    LocationId   = i.LocationId,
                    Quantity     = i.Quantity,
                    ReorderLevel = i.ReorderLevel,
                    LastUpdated  = i.LastUpdated
                })
                .ToListAsync();

            foreach (var item in lowStockItems)
                item.LocationName = item.LocationType == LocationType.Warehouse
                    ? (warehouses.TryGetValue(item.LocationId, out var wn) ? wn : "Warehouse")
                    : (stores.TryGetValue(item.LocationId, out var sn) ? sn : "Store");

            // ── Store dropdown list (for Admin/WarehouseManager filter) ──────────
            var allStores = await _db.Stores.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            string? selectedStoreName = storeId.HasValue
                ? allStores.FirstOrDefault(s => s.Id == storeId)?.Name
                : null;

            return new DashboardViewModel
            {
                TotalProducts    = totalProducts,
                TotalStores      = totalStores,
                TotalWarehouses  = totalWarehouses,
                TotalStockUnits  = totalStock,
                LowStockCount    = lowStockCount,
                TodaySalesAmount = todaySales?.Amount ?? 0,
                TodaySalesCount  = todaySales?.Count ?? 0,
                PendingTransfers = pendingTransfers,

                MonthlySalesLabels = monthlySales
                    .Select(m => new DateTime(m.Year, m.Month, 1).ToString("MMM yy")).ToList(),
                MonthlySalesData = monthlySales.Select(m => m.Total).ToList(),

                CategoryStockLabels = categoryStock.Select(c => c.Category).ToList(),
                CategoryStockData   = categoryStock.Select(c => c.Total).ToList(),

                RecentSales     = recentSales,
                RecentTransfers = recentTransfers,
                LowStockItems   = lowStockItems,

                SelectedStoreId   = storeId,
                SelectedStoreName = selectedStoreName,
                Stores = allStores.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList()
            };
        }
    }
}
