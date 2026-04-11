using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartStock.Models
{
    public class StockTransferItem
    {
        public int Id { get; set; }

        public int StockTransferId { get; set; }

        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // Navigation
        public StockTransfer StockTransfer { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
