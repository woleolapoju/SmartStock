using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _db;

        public StoreController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var stores = await _db.Stores.OrderBy(s => s.Name).ToListAsync();
            return View(stores);
        }

        public async Task<IActionResult> Details(int id)
        {
            var store = await _db.Stores.Include(s => s.Users).FirstOrDefaultAsync(s => s.Id == id);
            if (store == null) return NotFound();
            return View(store);
        }

        public IActionResult Create() => View(new StoreViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StoreViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            _db.Stores.Add(new Store
            {
                Name = model.Name,
                Address = model.Address,
                Phone = model.Phone,
                Email = model.Email,
                IsActive = model.IsActive
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Store created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var store = await _db.Stores.FindAsync(id);
            if (store == null) return NotFound();
            return View(new StoreViewModel
            {
                Id = store.Id, Name = store.Name, Address = store.Address,
                Phone = store.Phone, Email = store.Email, IsActive = store.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StoreViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var store = await _db.Stores.FindAsync(id);
            if (store == null) return NotFound();

            store.Name = model.Name;
            store.Address = model.Address;
            store.Phone = model.Phone;
            store.Email = model.Email;
            store.IsActive = model.IsActive;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Store updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var store = await _db.Stores.FindAsync(id);
            if (store != null) { store.IsActive = false; await _db.SaveChangesAsync(); }
            TempData["Success"] = "Store deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
