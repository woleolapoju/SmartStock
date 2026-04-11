using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models
{
    public class Store
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
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<StockTransfer> IncomingTransfers { get; set; } = new List<StockTransfer>();
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
