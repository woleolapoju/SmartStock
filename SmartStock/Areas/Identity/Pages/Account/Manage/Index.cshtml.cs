using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? StoreName { get; set; }
        public string Initials { get; set; } = "U";

        [TempData] public string? StatusMessage { get; set; }
        [TempData] public string? StatusType { get; set; }

        [BindProperty] public ProfileInputModel ProfileInput { get; set; } = new();
        [BindProperty] public PasswordInputModel PasswordInput { get; set; } = new();

        public class ProfileInputModel
        {
            [Required(ErrorMessage = "Full name is required.")]
            [MaxLength(100)]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;
        }

        public class PasswordInputModel
        {
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string? CurrentPassword { get; set; }

            [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
            public string? ConfirmPassword { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            Role = roles.FirstOrDefault() ?? "—";

            string? storeName = null;
            if (user.StoreId.HasValue)
                storeName = (await _db.Stores.FindAsync(user.StoreId.Value))?.Name;

            Email = user.Email ?? string.Empty;
            StoreName = storeName;
            Initials = string.IsNullOrEmpty(user.FullName)
                ? (user.Email?.FirstOrDefault().ToString().ToUpper() ?? "U")
                : string.Concat(user.FullName.Split(' ').Take(2).Select(w => w.FirstOrDefault().ToString().ToUpper()));

            ProfileInput = new ProfileInputModel { FullName = user.FullName ?? string.Empty };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");
            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            user.FullName = ProfileInput.FullName;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Profile updated successfully.";
                StatusType = "success";
            }
            else
            {
                StatusMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                StatusType = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            if (string.IsNullOrWhiteSpace(PasswordInput.CurrentPassword) ||
                string.IsNullOrWhiteSpace(PasswordInput.NewPassword))
            {
                StatusMessage = "Please fill in all password fields.";
                StatusType = "warning";
                await LoadAsync(user);
                return Page();
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var result = await _userManager.ChangePasswordAsync(user, PasswordInput.CurrentPassword!, PasswordInput.NewPassword!);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Password changed successfully.";
                StatusType = "success";
            }
            else
            {
                StatusMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                StatusType = "danger";
            }

            return RedirectToPage();
        }
    }
}
