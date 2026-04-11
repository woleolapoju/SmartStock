using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartStock.Models
{
    public class Purchase
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string ReferenceNumber { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        [MaxLength(150)]
        public string? SupplierName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public string? CreatedByUserId { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public DateTime? ReceivedDate { get; set; }

        // Navigation
        public Warehouse Warehouse { get; set; } = null!;
        public ApplicationUser? CreatedBy { get; set; }
        public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    }

    public enum PurchaseStatus
    {
        Pending = 1,
        Ordered = 2,
        Received = 3,
        Cancelled = 4
    }
}
