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
    public class SaleController : AppBaseController
    {
        private readonly ISaleService _saleService;
        private readonly ApplicationDbContext _db;
        private readonly ISystemParameterService _sysParams;

        public SaleController(ISaleService saleService, ApplicationDbContext db, ISystemParameterService sysParams)
        {
            _saleService = saleService;
            _db = db;
            _sysParams = sysParams;
        }

        // GET: Sale
        public async Task<IActionResult> Index(int? storeId, DateTime? from, DateTime? to)
        {
            bool locked = IsStoreRestricted();
            int? effectiveStoreId = locked ? GetUserStoreId() : storeId;

            var sales = await _saleService.GetAllAsync(effectiveStoreId, from, to);

            ViewBag.IsStoreLocked = locked;
            ViewBag.SelectedStoreId = effectiveStoreId;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To   = to?.ToString("yyyy-MM-dd");

            if (locked)
            {
                var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == effectiveStoreId);
                ViewBag.Stores    = store != null
                    ? new[] { new SelectListItem(store.Name, store.Id.ToString()) }
                    : Enumerable.Empty<SelectListItem>();
                ViewBag.StoreName = store?.Name;
            }
            else
            {
                var stores = await _db.Stores.Where(s => s.IsActive).ToListAsync();
                ViewBag.Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString()));
            }

            return View(sales);
        }

        // GET: Sale/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null) return NotFound();

            // StoreManager / Staff can only see sales from their assigned store
            if (IsStoreRestricted() && sale.StoreId != GetUserStoreId())
                return Forbid();

            return View(sale);
        }

        // GET: Sale/Create (POS)
        [Authorize(Roles = "Admin,StoreManager,Staff")]
        public async Task<IActionResult> Create()
        {
            int? forcedStoreId = IsStoreRestricted() ? GetUserStoreId() : null;
            var model = await BuildCreateViewModel(forcedStoreId);
            return View(model);
        }

        // POST: Sale/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,StoreManager,Staff")]
        public async Task<IActionResult> Create(CreateSaleViewModel model)
        {
            // Enforce store restriction on POST as well
            if (IsStoreRestricted())
            {
                var allowed = GetUserStoreId();
                if (allowed.HasValue) model.StoreId = allowed.Value;
            }

            if (!ModelState.IsValid || model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "Cart must have at least one item.");
                int? forcedId = IsStoreRestricted() ? GetUserStoreId() : null;
                var rebuilt = await BuildCreateViewModel(forcedId);
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
            var vm = await BuildCreateViewModel(IsStoreRestricted() ? GetUserStoreId() : null);
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

            if (IsStoreRestricted() && sale.StoreId != GetUserStoreId())
                return Forbid();

            return View(sale);
        }

        // API: get stock for a product at a store (used by POS JS)
        [HttpGet]
        public async Task<IActionResult> GetProductStock(int productId, int storeId)
        {
            var stock = await _db.Inventories
                .Where(i => i.ProductId == productId
                         && i.LocationType == Models.LocationType.Store
                         && i.LocationId == storeId)
                .Select(i => new { i.Quantity })
                .FirstOrDefaultAsync();
            var product = await _db.Products.FindAsync(productId);
            return Json(new { quantity = stock?.Quantity ?? 0, price = product?.Price ?? 0 });
        }

        private async Task<CreateSaleViewModel> BuildCreateViewModel(int? forcedStoreId = null)
        {
            var stores = forcedStoreId.HasValue
                ? await _db.Stores.Where(s => s.Id == forcedStoreId && s.IsActive).ToListAsync()
                : await _db.Stores.Where(s => s.IsActive).ToListAsync();

            var categories = await _db.Categories
                .Where(c => c.IsActive && c.Products.Any(p => p.IsActive))
                .OrderBy(c => c.Name).ToListAsync();

            var sysParam = await _sysParams.GetAsync();

            ViewBag.Categories    = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));
            ViewBag.IsStoreLocked = forcedStoreId.HasValue;
            ViewBag.TaxRate       = sysParam.TaxRate;

            return new CreateSaleViewModel
            {
                StoreId  = forcedStoreId ?? 0,
                Stores   = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString())),
                Products = new List<SelectListItem>()
            };
        }
    }
}
