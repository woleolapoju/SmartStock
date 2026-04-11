using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Interfaces;
using SmartStock.Models;

namespace SmartStock.Controllers.Api
{
    [ApiController]
    [Route("api/inventory")]
    [Authorize]
    public class ApiInventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public ApiInventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("warehouse/{warehouseId}")]
        public async Task<IActionResult> GetWarehouseInventory(int warehouseId)
        {
            var items = await _inventoryService.GetWarehouseInventoryAsync(warehouseId);
            return Ok(items);
        }

        [HttpGet("store/{storeId}")]
        public async Task<IActionResult> GetStoreInventory(int storeId)
        {
            var items = await _inventoryService.GetStoreInventoryAsync(storeId);
            return Ok(items);
        }

        [HttpGet("lowstock")]
        public async Task<IActionResult> GetLowStock()
        {
            var items = await _inventoryService.GetLowStockItemsAsync();
            return Ok(items);
        }

        [HttpGet("{productId}/{locationType}/{locationId}")]
        public async Task<IActionResult> GetStock(int productId, LocationType locationType, int locationId)
        {
            var inv = await _inventoryService.GetInventoryAsync(productId, locationType, locationId);
            if (inv == null) return NotFound();
            return Ok(new { inv.Quantity, inv.ReorderLevel, inv.LastUpdated });
        }
    }
}
