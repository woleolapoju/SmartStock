using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartStock.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Barcode { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Selling Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Cost Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost price must be greater than 0")]
        public decimal CostPrice { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        public string? ImageUrl { get; set; }

        // For dropdowns
        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    }

    public class ProductListViewModel
    {
        public IEnumerable<SmartStock.Models.Product> Products { get; set; } = new List<SmartStock.Models.Product>();
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
