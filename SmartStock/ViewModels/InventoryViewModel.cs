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
