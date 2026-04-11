using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PurchaseService> _logger;

        public PurchaseService(ApplicationDbContext db, ILogger<PurchaseService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<Purchase>> GetAllAsync() =>
            await _db.Purchases
                .Include(p => p.Warehouse)
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

        public async Task<Purchase?> GetByIdAsync(int id) =>
            await _db.Purchases
                .Include(p => p.Warehouse)
                .Include(p => p.CreatedBy)
                .Include(p => p.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<ServiceResult> CreateAsync(CreatePurchaseViewModel model, string userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var items = model.Items.Select(i => new PurchaseItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    LineTotal = i.UnitCost * i.Quantity
                }).ToList();

                var purchase = new Purchase
                {
                    ReferenceNumber = GenerateRef("PO"),
                    WarehouseId = model.WarehouseId,
                    SupplierName = model.SupplierName,
                    TotalAmount = items.Sum(i => i.LineTotal),
                    Status = PurchaseStatus.Pending,
                    Notes = model.Notes,
                    CreatedByUserId = userId,
                    PurchaseDate = DateTime.UtcNow,
                    Items = items
                };

                _db.Purchases.Add(purchase);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Purchase {Ref} created", purchase.ReferenceNumber);
                return ServiceResult.Ok($"Purchase order {purchase.ReferenceNumber} created.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error creating purchase");
                return ServiceResult.Fail("An error occurred creating the purchase order.");
            }
        }

        public async Task<ServiceResult> ReceiveAsync(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var purchase = await _db.Purchases
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (purchase == null) return ServiceResult.Fail("Purchase not found.");
                if (purchase.Status == PurchaseStatus.Received)
                    return ServiceResult.Fail("Purchase already received.");
                if (purchase.Status == PurchaseStatus.Cancelled)
                    return ServiceResult.Fail("Cannot receive a cancelled purchase.");

                // Add to warehouse inventory
                foreach (var item in purchase.Items)
                {
                    var stock = await _db.Inventories.FirstOrDefaultAsync(i =>
                        i.ProductId == item.ProductId &&
                        i.LocationType == LocationType.Warehouse &&
                        i.LocationId == purchase.WarehouseId);

                    if (stock == null)
                    {
                        stock = new Inventory
                        {
                            ProductId = item.ProductId,
                            LocationType = LocationType.Warehouse,
                            LocationId = purchase.WarehouseId,
                            Quantity = 0,
                            ReorderLevel = 20
                        };
                        _db.Inventories.Add(stock);
                    }

                    stock.Quantity += item.Quantity;
                    stock.LastUpdated = DateTime.UtcNow;
                }

                purchase.Status = PurchaseStatus.Received;
                purchase.ReceivedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Purchase {Id} received. Warehouse stock updated.", id);
                return ServiceResult.Ok("Purchase received. Warehouse inventory updated.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error receiving purchase {Id}", id);
                return ServiceResult.Fail("An error occurred receiving the purchase.");
            }
        }

        public async Task<ServiceResult> CancelAsync(int id)
        {
            var purchase = await _db.Purchases.FindAsync(id);
            if (purchase == null) return ServiceResult.Fail("Purchase not found.");
            if (purchase.Status == PurchaseStatus.Received)
                return ServiceResult.Fail("Cannot cancel a received purchase.");

            purchase.Status = PurchaseStatus.Cancelled;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok("Purchase cancelled.");
        }

        private static string GenerateRef(string prefix) =>
            $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }
}
