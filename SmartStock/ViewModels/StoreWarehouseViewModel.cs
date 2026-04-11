using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class StoreViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Store Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20), Phone]
        public string? Phone { get; set; }

        [MaxLength(100), EmailAddress]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class WarehouseViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Warehouse Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20), Phone]
        public string? Phone { get; set; }

        [MaxLength(100), EmailAddress]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
