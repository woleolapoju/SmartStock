using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Interfaces;

namespace SmartStock.Controllers
{
    [Authorize]
    public class DashboardController : AppBaseController
    {
        private readonly IDashboardService _dashboard;

        public DashboardController(IDashboardService dashboard)
        {
            _dashboard = dashboard;
        }

        public async Task<IActionResult> Index(int? storeId)
        {
            // StoreManager / Staff are locked to their assigned store — ignore query param
            int? effectiveStoreId = IsStoreRestricted() ? GetUserStoreId() : storeId;

            var model = await _dashboard.GetDashboardDataAsync(effectiveStoreId);
            ViewBag.IsStoreRestricted = IsStoreRestricted();
            return View(model);
        }
    }
}
