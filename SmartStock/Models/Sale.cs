using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartStock.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string ReferenceNumber { get; set; } = string.Empty;

        public int StoreId { get; set; }

        public string? CashierId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public SaleStatus Status { get; set; } = SaleStatus.Completed;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public Store Store { get; set; } = null!;
        public ApplicationUser? Cashier { get; set; }
        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    }

    public enum SaleStatus
    {
        Completed = 1,
        Refunded = 2,
        Voided = 3
    }
}
