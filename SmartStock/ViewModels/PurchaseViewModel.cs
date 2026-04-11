using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartStock.ViewModels
{
    public class CreatePurchaseViewModel
    {
        [Required]
        [Display(Name = "Warehouse")]
        public int WarehouseId { get; set; }

        [MaxLength(150)]
        [Display(Name = "Supplier Name")]
        public string? SupplierName { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<PurchaseItemViewModel> Items { get; set; } = new();

        // Dropdowns
        public IEnumerable<SelectListItem> Warehouses { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Products { get; set; } = new List<SelectListItem>();
    }

    public class PurchaseItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitCost { get; set; }

        public string? ProductName { get; set; }
    }
}
