using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartStock.ViewModels
{
    public class CreateSaleViewModel
    {
        [Required]
        [Display(Name = "Store")]
        public int StoreId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Cart must have at least one item")]
        public List<SaleItemViewModel> Items { get; set; } = new();

        // Dropdowns
        public IEnumerable<SelectListItem> Stores { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Products { get; set; } = new List<SelectListItem>();
    }

    public class SaleItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public decimal Discount { get; set; }

        public string? ProductName { get; set; }
        public int AvailableStock { get; set; }
    }
}
