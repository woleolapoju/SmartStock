using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Services
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SaleService> _logger;

        public SaleService(ApplicationDbContext db, ILogger<SaleService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<Sale>> GetAllAsync(int? storeId = null, DateTime? from = null, DateTime? to = null)
        {
            var query = _db.Sales
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .AsQueryable();

            if (storeId.HasValue) query = query.Where(s => s.StoreId == storeId.Value);
            if (from.HasValue) query = query.Where(s => s.SaleDate >= from.Value);
            if (to.HasValue) query = query.Where(s => s.SaleDate <= to.Value.AddDays(1));

            return await query.OrderByDescending(s => s.SaleDate).ToListAsync();
        }

        public async Task<Sale?> GetByIdAsync(int id) =>
            await _db.Sales
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<ServiceResult<Sale>> CreateAsync(CreateSaleViewModel model, string cashierId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Validate stock for each item
                foreach (var item in model.Items)
                {
                    var stock = await _db.Inventories.FirstOrDefaultAsync(i =>
                        i.ProductId == item.ProductId &&
                        i.LocationType == LocationType.Store &&
                        i.LocationId == model.StoreId);

                    if (stock == null || stock.Quantity < item.Quantity)
                    {
                        var product = await _db.Products.FindAsync(item.ProductId);
                        return ServiceResult<Sale>.Fail(
                            $"Insufficient stock for '{product?.Name ?? "Unknown"}'. Available: {stock?.Quantity ?? 0}");
                    }
                }

                var saleItems = model.Items.Select(i => new SaleItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount,
                    LineTotal = (i.UnitPrice * i.Quantity) - i.Discount
                }).ToList();

                var subTotal = saleItems.Sum(i => i.LineTotal);
                const decimal taxRate = 0.08m; // 8% tax
                var taxAmount = subTotal * taxRate;
                var total = subTotal + taxAmount - model.Discount;

                var sale = new Sale
                {
                    ReferenceNumber = GenerateRef("SALE"),
                    StoreId = model.StoreId,
                    CashierId = cashierId,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    Discount = model.Discount,
                    TotalAmount = total,
                    Status = SaleStatus.Completed,
                    Notes = model.Notes,
                    SaleDate = DateTime.UtcNow,
                    Items = saleItems
                };

                _db.Sales.Add(sale);
                await _db.SaveChangesAsync();

                // Deduct inventory
                foreach (var item in model.Items)
                {
                    var stock = await _db.Inventories.FirstAsync(i =>
                        i.ProductId == item.ProductId &&
                        i.LocationType == LocationType.Store &&
                        i.LocationId == model.StoreId);

                    stock.Quantity -= item.Quantity;
                    stock.LastUpdated = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Sale {Ref} completed. Total: {Total}", sale.ReferenceNumber, total);
                return ServiceResult<Sale>.Ok(sale, $"Sale {sale.ReferenceNumber} completed.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error creating sale");
                return ServiceResult<Sale>.Fail("An error occurred processing the sale.");
            }
        }

        public async Task<ServiceResult> VoidAsync(int id)
        {
            var sale = await _db.Sales.FindAsync(id);
            if (sale == null) return ServiceResult.Fail("Sale not found.");
            if (sale.Status == SaleStatus.Voided) return ServiceResult.Fail("Sale is already voided.");

            sale.Status = SaleStatus.Voided;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Sale voided.");
        }

        public async Task<IEnumerable<Sale>> GetRecentAsync(int count = 10) =>
            await _db.Sales
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .OrderByDescending(s => s.SaleDate)
                .Take(count)
                .ToListAsync();

        private static string GenerateRef(string prefix) =>
            $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }
}
