using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models
{
    public class Warehouse
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<StockTransfer> OutgoingTransfers { get; set; } = new List<StockTransfer>();
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}
