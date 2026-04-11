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

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var today = DateTime.UtcNow.Date;

            // KPIs — run in parallel
            var totalProductsTask = _db.Products.CountAsync(p => p.IsActive);
            var totalStoresTask = _db.Stores.CountAsync(s => s.IsActive);
            var totalWarehousesTask = _db.Warehouses.CountAsync(w => w.IsActive);
            var totalStockTask = _db.Inventories.SumAsync(i => (long)i.Quantity);
            var lowStockTask = _db.Inventories.CountAsync(i => i.Quantity <= i.ReorderLevel && i.Product.IsActive);
            var todaySalesTask = _db.Sales.Where(s => s.SaleDate >= today && s.Status == SaleStatus.Completed)
                .GroupBy(_ => 1)
                .Select(g => new { Amount = g.Sum(s => s.TotalAmount), Count = g.Count() })
                .FirstOrDefaultAsync();
            var pendingTransfersTask = _db.StockTransfers.CountAsync(t =>
                t.Status == TransferStatus.Pending || t.Status == TransferStatus.Approved);

//            await Task.WhenAll(totalProductsTask, totalStoresTask, totalWarehousesTask,
      //          totalStockTask, lowStockTask, todaySalesTask, pendingTransfersTask);

            var todaySales = await todaySalesTask;

            // Monthly sales chart — last 6 months
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var monthlySales = await _db.Sales
                .Where(s => s.SaleDate >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1) && s.Status == SaleStatus.Completed)
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(s => s.TotalAmount) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // Category stock chart
            var categoryStock = await _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.Product.IsActive)
                .GroupBy(i => i.Product.Category.Name)
                .Select(g => new { Category = g.Key, Total = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Total)
                .Take(6)
                .ToListAsync();

            // Recent sales and transfers
            var recentSales = await _db.Sales
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

            var recentTransfers = await _db.StockTransfers
                .Include(t => t.Warehouse)
                .Include(t => t.Store)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Low stock items
            var warehouses = await _db.Warehouses.ToDictionaryAsync(w => w.Id, w => w.Name);
            var stores = await _db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name);

            var lowStockItems = await _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.Quantity <= i.ReorderLevel && i.Product.IsActive)
                .OrderBy(i => i.Quantity)
                .Take(10)
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

            // Resolve location names
            foreach (var item in lowStockItems)
            {
                item.LocationName = item.LocationType == LocationType.Warehouse
                    ? (warehouses.TryGetValue(item.LocationId, out var wn) ? wn : "Warehouse")
                    : (stores.TryGetValue(item.LocationId, out var sn) ? sn : "Store");
            }

            return new DashboardViewModel
            {
                TotalProducts = await totalProductsTask,
                TotalStores = await totalStoresTask,
                TotalWarehouses = await totalWarehousesTask,
                TotalStockUnits = await totalStockTask,
                LowStockCount = await lowStockTask,
                TodaySalesAmount = todaySales?.Amount ?? 0,
                TodaySalesCount = todaySales?.Count ?? 0,
                PendingTransfers = await pendingTransfersTask,

                MonthlySalesLabels = monthlySales
                    .Select(m => new DateTime(m.Year, m.Month, 1).ToString("MMM yy"))
                    .ToList(),
                MonthlySalesData = monthlySales.Select(m => m.Total).ToList(),

                CategoryStockLabels = categoryStock.Select(c => c.Category).ToList(),
                CategoryStockData = categoryStock.Select(c => c.Total).ToList(),

                RecentSales = recentSales,
                RecentTransfers = recentTransfers,
                LowStockItems = lowStockItems
            };
        }
    }
}
