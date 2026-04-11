using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartStock.Models
{
    /// <summary>
    /// Tracks stock quantity for a product at a specific location (Warehouse or Store).
    /// LocationType discriminates between warehouse and store inventory.
    /// </summary>
    public class Inventory
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public LocationType LocationType { get; set; }

        /// <summary>Warehouse.Id or Store.Id depending on LocationType.</summary>
        public int LocationId { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderLevel { get; set; } = 10;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation
        public Product Product { get; set; } = null!;
        public Warehouse? Warehouse { get; set; }
        public Store? Store { get; set; }
    }

    public enum LocationType
    {
        Warehouse = 1,
        Store = 2
    }
}
