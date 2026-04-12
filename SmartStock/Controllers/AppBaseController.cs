using Microsoft.AspNetCore.Mvc;

namespace SmartStock.Controllers
{
    /// <summary>
    /// Shared helpers for store-scoped access control.
    /// Reads the "store_id" claim injected by CustomUserClaimsPrincipalFactory.
    /// </summary>
    public abstract class AppBaseController : Controller
    {
        /// <summary>Returns true if the current user is scoped to a single store.</summary>
        protected bool IsStoreRestricted() =>
            User.IsInRole("StoreManager") || User.IsInRole("Staff");

        /// <summary>Returns the store_id claim value, or null if the user has no store assignment.</summary>
        protected int? GetUserStoreId()
        {
            var claim = User.FindFirst("store_id")?.Value;
            return claim != null && int.TryParse(claim, out var id) ? id : null;
        }
    }
}
