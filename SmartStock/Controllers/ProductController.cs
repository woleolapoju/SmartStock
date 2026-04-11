using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartStock.Interfaces;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: Product
        public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
        {
            const int pageSize = 20;
            var products = (await _productService.GetAllAsync(search, categoryId)).ToList();
            var categories = await _productService.GetCategoriesAsync();

            var model = new ProductListViewModel
            {
                Products = products.Skip((page - 1) * pageSize).Take(pageSize),
                Search = search,
                CategoryId = categoryId,
                Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())),
                TotalCount = products.Count,
                Page = page,
                PageSize = pageSize
            };
            return View(model);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: Product/Create
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Create()
        {
            var categories = await _productService.GetCategoriesAsync();
            return View(new ProductViewModel
            {
                Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            });
        }

        // POST: Product/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = (await _productService.GetCategoriesAsync())
                    .Select(c => new SelectListItem(c.Name, c.Id.ToString()));
                return View(model);
            }

            var result = await _productService.CreateAsync(model);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err);
            model.Categories = (await _productService.GetCategoriesAsync())
                .Select(c => new SelectListItem(c.Name, c.Id.ToString()));
            return View(model);
        }

        // GET: Product/Edit/5
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _productService.GetCategoriesAsync();
            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Barcode = product.Barcode,
                Description = product.Description,
                Price = product.Price,
                CostPrice = product.CostPrice,
                CategoryId = product.CategoryId,
                IsActive = product.IsActive,
                ImageUrl = product.ImageUrl,
                Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            };
            return View(model);
        }

        // POST: Product/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> Edit(int id, ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = (await _productService.GetCategoriesAsync())
                    .Select(c => new SelectListItem(c.Name, c.Id.ToString()));
                return View(model);
            }

            var result = await _productService.UpdateAsync(id, model);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err);
            model.Categories = (await _productService.GetCategoriesAsync())
                .Select(c => new SelectListItem(c.Name, c.Id.ToString()));
            return View(model);
        }

        // POST: Product/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? result.Message : result.Errors.FirstOrDefault();
            return RedirectToAction(nameof(Index));
        }
    }
}
