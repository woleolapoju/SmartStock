using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartStock.Models;
using System.Security.Claims;

namespace SmartStock.Services
{
    /// <summary>
    /// Adds the user's assigned StoreId as a claim on login so controllers
    /// can read it from the principal without an extra DB round-trip.
    /// </summary>
    public class CustomUserClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public CustomUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            if (user.StoreId.HasValue)
                identity.AddClaim(new Claim("store_id", user.StoreId.Value.ToString()));
            return identity;
        }
    }
}
