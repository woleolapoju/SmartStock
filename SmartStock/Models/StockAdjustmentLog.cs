using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models
{
    public class StockAdjustmentLog
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public LocationType LocationType { get; set; }

        public int LocationId { get; set; }

        public int QuantityBefore { get; set; }

        public int QuantityChange { get; set; }

        public int QuantityAfter { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public string? AdjustedByUserId { get; set; }

        public DateTime AdjustedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Product Product { get; set; } = null!;
        public ApplicationUser? AdjustedBy { get; set; }
    }
}
