using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartStock.Models
{
    public class StockTransfer
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string ReferenceNumber { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public int StoreId { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public string? CreatedByUserId { get; set; }

        public string? ApprovedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? DispatchedAt { get; set; }

        public DateTime? ReceivedAt { get; set; }

        // Navigation
        public Warehouse Warehouse { get; set; } = null!;
        public Store Store { get; set; } = null!;
        public ApplicationUser? CreatedBy { get; set; }
        public ApplicationUser? ApprovedBy { get; set; }
        public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
    }

    public enum TransferStatus
    {
        Pending = 1,
        Approved = 2,
        Dispatched = 3,
        Received = 4,
        Cancelled = 5
    }
}
