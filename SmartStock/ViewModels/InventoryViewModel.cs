using SmartStock.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class InventoryViewModel
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public LocationType LocationType { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsLowStock => Quantity <= ReorderLevel;
        public DateTime LastUpdated { get; set; }

        // Valuation (used by the Inventory report)
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal CostValue => Quantity * CostPrice;
        public decimal SellingValue => Quantity * SellingPrice;
        public decimal ProfitLoss => SellingValue - CostValue;
    }

    public class InventoryListViewModel
    {
        public IEnumerable<InventoryViewModel> Items { get; set; } = new List<InventoryViewModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Filter / sort state (Warehouse & Store views)
        public int? CategoryId { get; set; }
        public string? Search { get; set; }
        public string Sort { get; set; } = "name";
        public string Dir { get; set; } = "asc";
        public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Categories { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    }

    public class StockAdjustmentViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public LocationType LocationType { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        [Range(-10000, 10000)]
        public int QuantityChange { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
