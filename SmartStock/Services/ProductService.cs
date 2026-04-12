using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext db, ILogger<ProductService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null, bool activeOnly = true)
        {
            var query = _db.Products.Include(p => p.Category).AsQueryable();

            if (activeOnly)
                query = query.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.SKU.Contains(search) ||
                    (p.Barcode != null && p.Barcode.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id) =>
            await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Product?> GetBySkuAsync(string sku) =>
            await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.SKU == sku);

        public async Task<ServiceResult> CreateAsync(ProductViewModel model)
        {
            // Auto-generate SKU if not provided
            if (string.IsNullOrWhiteSpace(model.SKU))
                model.SKU = await GenerateSkuAsync(model.CategoryId);

            model.SKU = model.SKU.ToUpper().Trim();

            if (await _db.Products.AnyAsync(p => p.SKU == model.SKU))
                return ServiceResult.Fail($"SKU '{model.SKU}' already exists.");

            var product = new Product
            {
                Name = model.Name,
                SKU = model.SKU,
                Barcode = model.Barcode,
                Description = model.Description,
                Price = model.Price,
                CostPrice = model.CostPrice,
                CategoryId = model.CategoryId,
                IsActive = model.IsActive,
                ImageUrl = model.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Product created: {SKU}", product.SKU);
            return ServiceResult.Ok("Product created successfully.");
        }

        public async Task<ServiceResult> UpdateAsync(int id, ProductViewModel model)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
                return ServiceResult.Fail("Product not found.");

            if (await _db.Products.AnyAsync(p => p.SKU == model.SKU && p.Id != id))
                return ServiceResult.Fail($"SKU '{model.SKU}' is already used by another product.");

            product.Name = model.Name;
            product.SKU = model.SKU.ToUpper().Trim();
            product.Barcode = model.Barcode;
            product.Description = model.Description;
            product.Price = model.Price;
            product.CostPrice = model.CostPrice;
            product.CategoryId = model.CategoryId;
            product.IsActive = model.IsActive;
            product.ImageUrl = model.ImageUrl;
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Product updated: {Id}", id);
            return ServiceResult.Ok("Product updated successfully.");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
                return ServiceResult.Fail("Product not found.");

            // Soft delete
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Product soft-deleted: {Id}", id);
            return ServiceResult.Ok("Product deactivated.");
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync() =>
            await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

        public Task<string> GenerateSkuForCategoryAsync(int categoryId) => GenerateSkuAsync(categoryId);

        private async Task<string> GenerateSkuAsync(int categoryId)
        {
            var category = await _db.Categories.FindAsync(categoryId);
            var raw = (category?.Name ?? "PROD").ToUpper().Replace(" ", "").Replace("&", "");
            var prefix = raw.Length >= 4 ? raw[..4] : raw.PadRight(4, 'X');

            // Find the highest numeric suffix for this prefix
            var existing = await _db.Products
                .Where(p => p.SKU.StartsWith(prefix + "-"))
                .Select(p => p.SKU)
                .ToListAsync();

            int nextNum = 1;
            foreach (var sku in existing)
            {
                var parts = sku.Split('-');
                if (parts.Length >= 2 && int.TryParse(parts[^1], out int n) && n >= nextNum)
                    nextNum = n + 1;
            }

            return $"{prefix}-{nextNum:D3}";
        }
    }
}
