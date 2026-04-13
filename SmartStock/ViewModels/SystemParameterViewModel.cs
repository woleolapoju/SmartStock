using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class SystemParameterViewModel
    {
        [Required(ErrorMessage = "Owner name is required.")]
        [MaxLength(200)]
        [Display(Name = "System Owner / Licensed To")]
        public string OwnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tax rate is required.")]
        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
        [Display(Name = "Tax Rate (%)")]
        public decimal TaxRate { get; set; }

        [Required(ErrorMessage = "Currency symbol is required.")]
        [MaxLength(5)]
        [Display(Name = "Currency Symbol")]
        public string CurrencySymbol { get; set; } = "$";

        [Required(ErrorMessage = "Currency code is required.")]
        [MaxLength(10)]
        [Display(Name = "Currency Code")]
        public string CurrencyCode { get; set; } = "USD";

        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
