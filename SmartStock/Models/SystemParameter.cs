using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models
{
    public class SystemParameter
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string OwnerName { get; set; } = "SmartStock";

        /// <summary>Tax rate as a percentage, e.g. 8 = 8%.</summary>
        [Range(0, 100)]
        public decimal TaxRate { get; set; } = 8.0m;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? UpdatedByUserId { get; set; }
        public ApplicationUser? UpdatedBy { get; set; }
    }
}
