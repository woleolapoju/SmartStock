using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Interfaces;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SystemParameterController : Controller
    {
        private readonly ISystemParameterService _service;

        public SystemParameterController(ISystemParameterService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var param = await _service.GetAsync();
            var vm = new SystemParameterViewModel
            {
                OwnerName = param.OwnerName,
                TaxRate = param.TaxRate,
                CurrencySymbol = param.CurrencySymbol,
                CurrencyCode = param.CurrencyCode,
                LastUpdatedAt = param.UpdatedAt,
                LastUpdatedBy = param.UpdatedBy?.FullName ?? param.UpdatedBy?.Email
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SystemParameterViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            await _service.SaveAsync(new SystemParameter
            {
                OwnerName = model.OwnerName,
                TaxRate = model.TaxRate,
                CurrencySymbol = model.CurrencySymbol,
                CurrencyCode = model.CurrencyCode
            }, userId);

            TempData["Success"] = "System parameters saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
