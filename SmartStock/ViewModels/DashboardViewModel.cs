using SmartStock.Models;

namespace SmartStock.ViewModels
{
    public class DashboardViewModel
    {
        // KPI Cards
        public int TotalProducts { get; set; }
        public int TotalStores { get; set; }
        public int TotalWarehouses { get; set; }
        public long TotalStockUnits { get; set; }
        public int LowStockCount { get; set; }
        public decimal TodaySalesAmount { get; set; }
        public int TodaySalesCount { get; set; }
        public int PendingTransfers { get; set; }

        // Charts
        public List<string> MonthlySalesLabels { get; set; } = new();
        public List<decimal> MonthlySalesData { get; set; } = new();
        public List<string> CategoryStockLabels { get; set; } = new();
        public List<int> CategoryStockData { get; set; } = new();

        // Recent activity
        public List<Sale> RecentSales { get; set; } = new();
        public List<StockTransfer> RecentTransfers { get; set; } = new();
        public List<InventoryViewModel> LowStockItems { get; set; } = new();
    }
}
