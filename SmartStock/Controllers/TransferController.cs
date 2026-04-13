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
    public class TransferController : AppBaseController
    {
        private readonly ITransferService _transferService;
        private readonly ApplicationDbContext _db;

        public TransferController(ITransferService transferService, ApplicationDbContext db)
        {
            _transferService = transferService;
            _db = db;
        }

        // GET: Transfer
        public async Task<IActionResult> Index()
        {
            var transfers = await _transferService.GetAllAsync();

            // StoreManager / Staff see only transfers going to their store
            if (IsStoreRestricted())
            {
                var sid = GetUserStoreId();
                transfers = transfers.Where(t => t.StoreId == sid);
            }

            return View(transfers);
        }

        // GET: Transfer/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var transfer = await _transferService.GetByIdAsync(id);
            if (transfer == null) return NotFound();

            // StoreManager / Staff can only see transfers to their store
            if (IsStoreRestricted() && transfer.StoreId != GetUserStoreId())
                return Forbid();

            return View(transfer);
        }

        // GET: Transfer/Create
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Create()
        {
            var model = await BuildCreateViewModel();
            return View(model);
        }

        // POST: Transfer/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Create(StockTransferViewModel model)
        {
            if (!ModelState.IsValid || model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "At least one item is required.");
                var rebuilt = await BuildCreateViewModel();
                model.Warehouses = rebuilt.Warehouses;
                model.Stores = rebuilt.Stores;
                model.Products = rebuilt.Products;
                return View(model);
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var result = await _transferService.CreateAsync(model, userId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err);
            var vm = await BuildCreateViewModel();
            model.Warehouses = vm.Warehouses;
            model.Stores = vm.Stores;
            model.Products = vm.Products;
            return View(model);
        }

        // POST: Transfer/Approve/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var result = await _transferService.ApproveAsync(id, userId);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Transfer/Dispatch/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Dispatch(int id)
        {
            var result = await _transferService.DispatchAsync(id);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Transfer/Receive/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager,StoreManager")]
        public async Task<IActionResult> Receive(int id)
        {
            var result = await _transferService.ReceiveAsync(id);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Transfer/Cancel/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _transferService.CancelAsync(id);
            TempData[result.Success ? "Success" : "Error"] =
                result.Success ? result.Message : result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<StockTransferViewModel> BuildCreateViewModel()
        {
            var warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            var stores = await _db.Stores.Where(s => s.IsActive).ToListAsync();
            var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            var categories = await _db.Categories
                .Where(c => c.IsActive && c.Products.Any(p => p.IsActive &&
                    p.Inventories.Any(i => i.LocationType == Models.LocationType.Warehouse && i.Quantity > 1)))
                .OrderBy(c => c.Name).ToListAsync();

            ViewBag.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));

            return new StockTransferViewModel
            {
                Warehouses = warehouses.Select(w => new SelectListItem(w.Name, w.Id.ToString())),
                Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString())),
                Products = products.Select(p => new SelectListItem($"{p.Name} ({p.SKU})", p.Id.ToString()))
            };
        }
    }
}
