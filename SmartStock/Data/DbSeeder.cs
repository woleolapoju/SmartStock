using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStock.Models;

namespace SmartStock.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

         //   await db.Database.MigrateAsync();

            // ── Roles ─────────────────────────────────────────────────────────────
            string[] roles = ["Admin", "WarehouseManager", "StoreManager", "Staff"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── SystemParameter ───────────────────────────────────────────────────
            if (!await db.SystemParameters.AnyAsync())
            {
                db.SystemParameters.Add(new SystemParameter
                {
                    OwnerName = "Exel Chemist",
                    TaxRate = 8.0m,
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            // ── Categories ────────────────────────────────────────────────────────
            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Electronics", Description = "Electronic devices and accessories" },
                    new Category { Name = "Clothing", Description = "Apparel and fashion items" },
                    new Category { Name = "Food & Beverages", Description = "Consumable products" },
                    new Category { Name = "Home & Garden", Description = "Home improvement and garden supplies" },
                    new Category { Name = "Sports & Outdoors", Description = "Sports equipment and outdoor gear" }
                );
                await db.SaveChangesAsync();
            }

            // ── Warehouse ─────────────────────────────────────────────────────────
            if (!await db.Warehouses.AnyAsync())
            {
                db.Warehouses.AddRange(
                    new Warehouse { Name = "Central Warehouse", Address = "123 Industrial Park, Main City", Phone = "+1-555-0100", Email = "warehouse@smartstock.com" },
                    new Warehouse { Name = "North Distribution Hub", Address = "456 North Road, Metro Area", Phone = "+1-555-0101", Email = "north@smartstock.com" }
                );
                await db.SaveChangesAsync();
            }

            // ── Stores ────────────────────────────────────────────────────────────
            if (!await db.Stores.AnyAsync())
            {
                db.Stores.AddRange(
                    new Store { Name = "Downtown Store", Address = "789 Main Street, Downtown", Phone = "+1-555-0200", Email = "downtown@smartstock.com" },
                    new Store { Name = "Uptown Store", Address = "321 High Street, Uptown", Phone = "+1-555-0201", Email = "uptown@smartstock.com" },
                    new Store { Name = "Mall Outlet", Address = "654 Shopping Mall, West Side", Phone = "+1-555-0202", Email = "mall@smartstock.com" }
                );
                await db.SaveChangesAsync();
            }

            // ── Products ──────────────────────────────────────────────────────────
            if (!await db.Products.AnyAsync())
            {
                var electronics = await db.Categories.FirstAsync(c => c.Name == "Electronics");
                var clothing = await db.Categories.FirstAsync(c => c.Name == "Clothing");
                var food = await db.Categories.FirstAsync(c => c.Name == "Food & Beverages");
                var home = await db.Categories.FirstAsync(c => c.Name == "Home & Garden");
                var sports = await db.Categories.FirstAsync(c => c.Name == "Sports & Outdoors");

                db.Products.AddRange(
                    new Product { Name = "Wireless Headphones", SKU = "ELEC-001", Barcode = "7891234560001", Price = 79.99m, CostPrice = 40.00m, CategoryId = electronics.Id },
                    new Product { Name = "USB-C Charging Cable", SKU = "ELEC-002", Barcode = "7891234560002", Price = 12.99m, CostPrice = 4.50m, CategoryId = electronics.Id },
                    new Product { Name = "Bluetooth Speaker", SKU = "ELEC-003", Barcode = "7891234560003", Price = 49.99m, CostPrice = 22.00m, CategoryId = electronics.Id },
                    new Product { Name = "Men's T-Shirt", SKU = "CLTH-001", Barcode = "7891234560004", Price = 24.99m, CostPrice = 8.00m, CategoryId = clothing.Id },
                    new Product { Name = "Women's Jeans", SKU = "CLTH-002", Barcode = "7891234560005", Price = 59.99m, CostPrice = 20.00m, CategoryId = clothing.Id },
                    new Product { Name = "Coffee Beans 1kg", SKU = "FOOD-001", Barcode = "7891234560006", Price = 19.99m, CostPrice = 9.00m, CategoryId = food.Id },
                    new Product { Name = "Mineral Water 500ml", SKU = "FOOD-002", Barcode = "7891234560007", Price = 1.99m, CostPrice = 0.50m, CategoryId = food.Id },
                    new Product { Name = "Garden Hose 50ft", SKU = "HOME-001", Barcode = "7891234560008", Price = 34.99m, CostPrice = 14.00m, CategoryId = home.Id },
                    new Product { Name = "Yoga Mat", SKU = "SPRT-001", Barcode = "7891234560009", Price = 29.99m, CostPrice = 11.00m, CategoryId = sports.Id },
                    new Product { Name = "Water Bottle 1L", SKU = "SPRT-002", Barcode = "7891234560010", Price = 14.99m, CostPrice = 5.00m, CategoryId = sports.Id }
                );
                await db.SaveChangesAsync();
            }

            // ── Inventory ─────────────────────────────────────────────────────────
            if (!await db.Inventories.AnyAsync())
            {
                var warehouse = await db.Warehouses.FirstAsync();
                var products = await db.Products.ToListAsync();

                foreach (var product in products)
                {
                    db.Inventories.Add(new Inventory
                    {
                        ProductId = product.Id,
                        LocationType = LocationType.Warehouse,
                        LocationId = warehouse.Id,
                        Quantity = 100,
                        ReorderLevel = 20
                    });
                }
                await db.SaveChangesAsync();
            }

            // ── Admin User ────────────────────────────────────────────────────────
            if (await userManager.FindByEmailAsync("admin@smartstock.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@smartstock.com",
                    Email = "admin@smartstock.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ── Warehouse Manager ─────────────────────────────────────────────────
            if (await userManager.FindByEmailAsync("warehouse@smartstock.com") == null)
            {
                var wm = new ApplicationUser
                {
                    UserName = "warehouse@smartstock.com",
                    Email = "warehouse@smartstock.com",
                    FullName = "Warehouse Manager",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(wm, "Warehouse@123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(wm, "WarehouseManager");
            }

            // ── Store Manager ─────────────────────────────────────────────────────
            if (await userManager.FindByEmailAsync("store@smartstock.com") == null)
            {
                var store = await db.Stores.FirstAsync();
                var sm = new ApplicationUser
                {
                    UserName = "store@smartstock.com",
                    Email = "store@smartstock.com",
                    FullName = "Store Manager",
                    StoreId = store.Id,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(sm, "Store@123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(sm, "StoreManager");
            }
        }
    }
}
