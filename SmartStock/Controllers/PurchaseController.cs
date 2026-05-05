using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Interfaces;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly IPurchaseService _purchaseService;
        private readonly ApplicationDbContext _db;

        public PurchaseController(IPurchaseService purchaseService, ApplicationDbContext db)
        {
            _purchaseService = purchaseService;
            _db = db;
        }

        // GET: Purchase
        public async Task<IActionResult> Index()
        {
            var purchases = await _purchaseService.GetAllAsync();
            return View(purchases);
        }

        // GET: Purchase/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var purchase = await _purchaseService.GetByIdAsync(id);
            if (purchase == null) return NotFound();
            return View(purchase);
        }

        // GET: Purchase/Create
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Create()
        {
            var model = await BuildCreateViewModel();
            return View(model);
        }

        // POST: Purchase/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Create(CreatePurchaseViewModel model)
        {
            if (!ModelState.IsValid || model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "At least one item is required.");
                var rebuilt = await BuildCreateViewModel();
                model.Warehouses = rebuilt.Warehouses;
                model.Products = rebuilt.Products;
                return View(model);
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var result = await _purchaseService.CreateAsync(model, userId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err);
            var vm = await BuildCreateViewModel();
            model.Warehouses = vm.Warehouses;
            model.Products = vm.Products;
            return View(model);
        }

        // GET: Purchase/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var purchase = await _purchaseService.GetByIdAsync(id);
            if (purchase == null) return NotFound();
            return View(purchase);
        }

        // GET: Purchase/Edit/5
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var purchase = await _purchaseService.GetByIdAsync(id);
            if (purchase == null) return NotFound();
            if (purchase.Status != SmartStock.Models.PurchaseStatus.Pending)
            {
                TempData["Error"] = "Only pending purchase orders can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var vm = await BuildCreateViewModel();
            vm.Id = purchase.Id;
            vm.WarehouseId = purchase.WarehouseId;
            vm.SupplierName = purchase.SupplierName;
            vm.Notes = purchase.Notes;
            vm.Items = purchase.Items.Select(i => new ViewModels.PurchaseItemViewModel
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                ProductName = i.Product?.Name
            }).ToList();

            return View(vm);
        }

        // POST: Purchase/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Edit(int id, CreatePurchaseViewModel model)
        {
            if (!ModelState.IsValid || model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "At least one item is required.");
                var rebuilt = await BuildCreateViewModel();
                model.Warehouses = rebuilt.Warehouses;
                model.Products = rebuilt.Products;
                return View(model);
            }

            var result = await _purchaseService.UpdateAsync(id, model);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err);
            var vm = await BuildCreateViewModel();
            model.Warehouses = vm.Warehouses;
            model.Products = vm.Products;
            return View(model);
        }

        // POST: Purchase/Receive/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Receive(int id)
        {
            var result = await _purchaseService.ReceiveAsync(id);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Purchase/Cancel/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _purchaseService.CancelAsync(id);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<CreatePurchaseViewModel> BuildCreateViewModel()
        {
            var warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            var categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            ViewBag.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));

            return new CreatePurchaseViewModel
            {
                Warehouses = warehouses.Select(w => new SelectListItem(w.Name, w.Id.ToString())),
                Products = products.Select(p => new SelectListItem($"{p.Name} ({p.SKU})", p.Id.ToString()))
            };
        }
    }
}
