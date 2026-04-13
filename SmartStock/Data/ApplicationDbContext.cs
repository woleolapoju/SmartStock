using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartStock.Models;

namespace SmartStock.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<StockAdjustmentLog> StockAdjustmentLogs { get; set; }
        public DbSet<SystemParameter> SystemParameters { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Product ──────────────────────────────────────────────────────────
            builder.Entity<Product>(e =>
            {
                e.HasIndex(p => p.SKU).IsUnique();
                e.HasIndex(p => p.Barcode);
                e.HasIndex(p => p.CategoryId);
                e.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Inventory ─────────────────────────────────────────────────────────
            // LocationId is a polymorphic FK (Warehouse.Id or Store.Id) — no DB FK constraint;
            // integrity enforced at the service layer via LocationType discriminator.
            builder.Entity<Inventory>(e =>
            {
                e.HasIndex(i => new { i.ProductId, i.LocationType, i.LocationId }).IsUnique();
                e.HasIndex(i => new { i.LocationType, i.LocationId });

                e.HasOne(i => i.Product)
                    .WithMany(p => p.Inventories)
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Both Warehouse and Store nav props are NOT mapped as FKs —
                // they are resolved manually in queries via LocationType discriminator.
                e.Ignore(i => i.Warehouse);
                e.Ignore(i => i.Store);
            });

            // ── StockTransfer ─────────────────────────────────────────────────────
            builder.Entity<StockTransfer>(e =>
            {
                e.HasIndex(t => t.ReferenceNumber).IsUnique();

                e.HasOne(t => t.Warehouse)
                    .WithMany(w => w.OutgoingTransfers)
                    .HasForeignKey(t => t.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Store)
                    .WithMany(s => s.IncomingTransfers)
                    .HasForeignKey(t => t.StoreId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.CreatedBy)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(t => t.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(t => t.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── StockTransferItem ─────────────────────────────────────────────────
            builder.Entity<StockTransferItem>(e =>
            {
                e.HasIndex(i => new { i.StockTransferId, i.ProductId });

                e.HasOne(i => i.StockTransfer)
                    .WithMany(t => t.Items)
                    .HasForeignKey(i => i.StockTransferId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(i => i.Product)
                    .WithMany(p => p.StockTransferItems)
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Sale ──────────────────────────────────────────────────────────────
            builder.Entity<Sale>(e =>
            {
                e.HasIndex(s => s.ReferenceNumber).IsUnique();
                e.HasIndex(s => s.SaleDate);

                e.HasOne(s => s.Store)
                    .WithMany(st => st.Sales)
                    .HasForeignKey(s => s.StoreId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.Cashier)
                    .WithMany()
                    .HasForeignKey(s => s.CashierId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── SaleItem ──────────────────────────────────────────────────────────
            builder.Entity<SaleItem>(e =>
            {
                e.HasIndex(i => new { i.SaleId, i.ProductId });

                e.HasOne(i => i.Sale)
                    .WithMany(s => s.Items)
                    .HasForeignKey(i => i.SaleId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(i => i.Product)
                    .WithMany(p => p.SaleItems)
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Purchase ──────────────────────────────────────────────────────────
            builder.Entity<Purchase>(e =>
            {
                e.HasIndex(p => p.ReferenceNumber).IsUnique();
                e.HasIndex(p => p.PurchaseDate);

                e.HasOne(p => p.Warehouse)
                    .WithMany(w => w.Purchases)
                    .HasForeignKey(p => p.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.CreatedBy)
                    .WithMany()
                    .HasForeignKey(p => p.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── PurchaseItem ──────────────────────────────────────────────────────
            builder.Entity<PurchaseItem>(e =>
            {
                e.HasIndex(i => new { i.PurchaseId, i.ProductId });

                e.HasOne(i => i.Purchase)
                    .WithMany(p => p.Items)
                    .HasForeignKey(i => i.PurchaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(i => i.Product)
                    .WithMany(p => p.PurchaseItems)
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── StockAdjustmentLog ────────────────────────────────────────────────
            builder.Entity<StockAdjustmentLog>(e =>
            {
                e.HasIndex(l => new { l.ProductId, l.AdjustedAt });
                e.HasIndex(l => new { l.LocationType, l.LocationId });

                e.HasOne(l => l.Product)
                    .WithMany()
                    .HasForeignKey(l => l.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(l => l.AdjustedBy)
                    .WithMany()
                    .HasForeignKey(l => l.AdjustedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ── SystemParameter ───────────────────────────────────────────────────
            builder.Entity<SystemParameter>(e =>
            {
                e.Property(sp => sp.TaxRate).HasPrecision(5, 2);

                e.HasOne(sp => sp.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(sp => sp.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasData(new SystemParameter
                {
                    Id = 1,
                    OwnerName = "SmartStock",
                    TaxRate = 8.0m,
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            });

            // ── ApplicationUser ───────────────────────────────────────────────────
            builder.Entity<ApplicationUser>(e =>
            {
                e.HasOne(u => u.Store)
                    .WithMany(s => s.Users)
                    .HasForeignKey(u => u.StoreId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure IdentityRole Id column type
            builder.Entity<IdentityRole>(entity =>
            {
                entity.Property(r => r.Id).HasColumnType("nvarchar(450)");
            });
        }
    }
}
