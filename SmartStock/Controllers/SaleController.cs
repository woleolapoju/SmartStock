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
    public class SaleController : Controller
    {
        private readonly ISaleService _saleService;
        private readonly ApplicationDbContext _db;

        public SaleController(ISaleService saleService, ApplicationDbContext db)
        {
            _saleService = saleService;
            _db = db;
        }

        // GET: Sale
        public async Task<IActionResult> Index(int? storeId, DateTime? from, DateTime? to)
        {
            var sales = await _saleService.GetAllAsync(storeId, from, to);
            var stores = await _db.Stores.Where(s => s.IsActive).ToListAsync();

            ViewBag.Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString()));
            ViewBag.SelectedStoreId = storeId;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(sales);
        }

        // GET: Sale/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null) return NotFound();
            return View(sale);
        }

        // GET: Sale/Create (POS)
        [Authorize(Roles = "Admin,StoreManager,Staff")]
        public async Task<IActionResult> Create()
        {
            var model = await BuildCreateViewModel();
            return View(model);
        }

        // POST: Sale/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,StoreManager,Staff")]
        public async Task<IActionResult> Create(CreateSaleViewModel model)
        {
            if (!ModelState.IsValid || model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "Cart must have at least one item.");
                var rebuilt = await BuildCreateViewModel();
                model.Stores = rebuilt.Stores;
                model.Products = rebuilt.Products;
                return View(model);
            }

            var cashierId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var result = await _saleService.CreateAsync(model, cashierId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err);
            var vm = await BuildCreateViewModel();
            model.Stores = vm.Stores;
            model.Products = vm.Products;
            return View(model);
        }

        // POST: Sale/Void/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,StoreManager")]
        public async Task<IActionResult> Void(int id)
        {
            var result = await _saleService.VoidAsync(id);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Sale/Receipt/5
        public async Task<IActionResult> Receipt(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null) return NotFound();
            return View(sale);
        }

        // API: get stock for a product at a store (used by POS JS)
        [HttpGet]
        public async Task<IActionResult> GetProductStock(int productId, int storeId)
        {
            var stock = await _db.Inventories
                .Where(i => i.ProductId == productId && i.LocationType == Models.LocationType.Store && i.LocationId == storeId)
                .Select(i => new { i.Quantity })
                .FirstOrDefaultAsync();
            var product = await _db.Products.FindAsync(productId);
            return Json(new { quantity = stock?.Quantity ?? 0, price = product?.Price ?? 0 });
        }

        private async Task<CreateSaleViewModel> BuildCreateViewModel()
        {
            var stores = await _db.Stores.Where(s => s.IsActive).ToListAsync();
            var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            var categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            ViewBag.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));

            return new CreateSaleViewModel
            {
                Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString())),
                Products = products.Select(p => new SelectListItem($"{p.Name} ({p.SKU})", p.Id.ToString()))
            };
        }
    }
}
