using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize(Roles = "Admin,WarehouseManager")]
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _db;

        public WarehouseController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var warehouses = await _db.Warehouses.OrderBy(w => w.Name).ToListAsync();
            return View(warehouses);
        }

        public async Task<IActionResult> Details(int id)
        {
            var warehouse = await _db.Warehouses.Include(w => w.Purchases).FirstOrDefaultAsync(w => w.Id == id);
            if (warehouse == null) return NotFound();
            return View(warehouse);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new WarehouseViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(WarehouseViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            _db.Warehouses.Add(new Warehouse
            {
                Name = model.Name, Address = model.Address,
                Phone = model.Phone, Email = model.Email, IsActive = model.IsActive
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Warehouse created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var wh = await _db.Warehouses.FindAsync(id);
            if (wh == null) return NotFound();
            return View(new WarehouseViewModel
            {
                Id = wh.Id, Name = wh.Name, Address = wh.Address,
                Phone = wh.Phone, Email = wh.Email, IsActive = wh.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, WarehouseViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var wh = await _db.Warehouses.FindAsync(id);
            if (wh == null) return NotFound();

            wh.Name = model.Name; wh.Address = model.Address;
            wh.Phone = model.Phone; wh.Email = model.Email; wh.IsActive = model.IsActive;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Warehouse updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
