using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;

namespace SmartStock.Controllers.Api
{
    [ApiController]
    [Route("api/products")]
    [Authorize]
    public class ApiProductController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductService _productService;

        public ApiProductController(ApplicationDbContext db, IProductService productService)
        {
            _db = db;
            _productService = productService;
        }

        /// <summary>Products for sale at a store — only those with qty > 0, optionally filtered by category.</summary>
        [HttpGet("for-sale")]
        public async Task<IActionResult> ForSale([FromQuery] int storeId, [FromQuery] int? categoryId)
        {
            var query = _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.LocationType == Models.LocationType.Store
                         && i.LocationId == storeId
                         && i.Quantity > 0
                         && i.Product.IsActive);

            if (categoryId.HasValue)
                query = query.Where(i => i.Product.CategoryId == categoryId.Value);

            var items = await query
                .OrderBy(i => i.Product.Name)
                .Select(i => new
                {
                    id = i.ProductId,
                    name = i.Product.Name,
                    sku = i.Product.SKU,
                    price = i.Product.Price,
                    available = i.Quantity,
                    category = i.Product.Category.Name
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>Products for transfer from a warehouse — only those with qty > 0, optionally filtered by category.</summary>
        [HttpGet("for-transfer")]
        public async Task<IActionResult> ForTransfer([FromQuery] int warehouseId, [FromQuery] int? categoryId)
        {
            var query = _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.LocationType == Models.LocationType.Warehouse
                         && i.LocationId == warehouseId
                         && i.Quantity > 0
                         && i.Product.IsActive);

            if (categoryId.HasValue)
                query = query.Where(i => i.Product.CategoryId == categoryId.Value);

            var items = await query
                .OrderBy(i => i.Product.Name)
                .Select(i => new
                {
                    id = i.ProductId,
                    name = i.Product.Name,
                    sku = i.Product.SKU,
                    available = i.Quantity,
                    category = i.Product.Category.Name
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>All active products for purchase order, optionally filtered by category.</summary>
        [HttpGet("for-purchase")]
        public async Task<IActionResult> ForPurchase([FromQuery] int? categoryId)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var items = await query
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    sku = p.SKU,
                    costPrice = p.CostPrice,
                    category = p.Category.Name
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>Generate a SKU preview for a given category.</summary>
        [HttpGet("generate-sku")]
        public async Task<IActionResult> GenerateSku([FromQuery] int categoryId)
        {
            var sku = await _productService.GenerateSkuForCategoryAsync(categoryId);
            return Ok(new { sku });
        }

        /// <summary>Quick-register a new product inline during a purchase order.</summary>
        [HttpPost("quick-create")]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> QuickCreate([FromBody] QuickCreateProductRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.CategoryId <= 0)
                return BadRequest(new { error = "Name and Category are required." });

            var vm = new SmartStock.ViewModels.ProductViewModel
            {
                Name = dto.Name.Trim(),
                SKU = dto.SKU ?? "",
                Price = dto.Price,
                CostPrice = dto.CostPrice,
                CategoryId = dto.CategoryId,
                IsActive = true
            };

            var result = await _productService.CreateAsync(vm);
            if (!result.Success)
                return BadRequest(new { error = result.Errors.FirstOrDefault() });

            // Retrieve the newly created product by SKU (SKU is unique)
            var product = await _db.Products
                .Where(p => p.Name == vm.Name && p.CategoryId == vm.CategoryId)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                id = product?.Id ?? 0,
                name = product?.Name ?? vm.Name,
                sku = product?.SKU ?? vm.SKU,
                costPrice = product?.CostPrice ?? vm.CostPrice
            });
        }

        public class QuickCreateProductRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? SKU { get; set; }
            public decimal Price { get; set; }
            public decimal CostPrice { get; set; }
            public int CategoryId { get; set; }
        }
    }
}
