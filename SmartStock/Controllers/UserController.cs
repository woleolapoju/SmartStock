using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.ViewModels;

namespace SmartStock.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        // GET: User
        public async Task<IActionResult> Index(string? search, string? role, bool? active)
        {
            var users = await _db.Users
                .Include(u => u.Store)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var storeNames = await _db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name);

            var list = new List<UserListViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                list.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault() ?? "—",
                    StoreName = user.StoreId.HasValue && storeNames.TryGetValue(user.StoreId.Value, out var sn) ? sn : null,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            }

            // Filters
            if (!string.IsNullOrWhiteSpace(search))
                list = list.Where(u =>
                    u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(role))
                list = list.Where(u => u.Role == role).ToList();

            if (active.HasValue)
                list = list.Where(u => u.IsActive == active.Value).ToList();

            ViewBag.Search = search;
            ViewBag.SelectedRole = role;
            ViewBag.ActiveFilter = active;
            ViewBag.RoleOptions = _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem(r.Name, r.Name));

            return View(list);
        }

        // GET: User/Create
        public async Task<IActionResult> Create()
        {
            return View(await BuildViewModel(null));
        }

        // POST: User/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "Password is required.");

            if (!ModelState.IsValid)
                return View(await BuildViewModel(model));

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim().ToLower(),
                Email = model.Email.Trim().ToLower(),
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber,
                StoreId = model.StoreId,
                IsActive = model.IsActive,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password!);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);
                return View(await BuildViewModel(model));
            }

            if (!string.IsNullOrWhiteSpace(model.Role))
                await _userManager.AddToRoleAsync(user, model.Role);

            TempData["Success"] = $"User '{user.FullName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: User/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var vm = new UserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "",
                StoreId = user.StoreId,
                IsActive = user.IsActive
            };

            return View(await BuildViewModel(vm));
        }

        // POST: User/Edit/id
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel model)
        {
            // Password fields optional on edit — strip password validators
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));

            if (!ModelState.IsValid)
                return View(await BuildViewModel(model));

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent the only admin from being demoted or deactivated
            if (user.Id == id && model.Role != "Admin")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count == 1 && admins[0].Id == id)
                {
                    ModelState.AddModelError("", "Cannot change the role of the only Admin account.");
                    return View(await BuildViewModel(model));
                }
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber;
            user.StoreId = model.StoreId;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                    ModelState.AddModelError("", err.Description);
                return View(await BuildViewModel(model));
            }

            // Sync role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrWhiteSpace(model.Role))
                await _userManager.AddToRoleAsync(user, model.Role);

            TempData["Success"] = $"User '{user.FullName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: User/ToggleActive/id
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent deactivating the only admin
            if (user.IsActive)  // about to deactivate
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count == 1 && admins[0].Id == id)
                {
                    TempData["Error"] = "Cannot deactivate the only Admin account.";
                    return RedirectToAction(nameof(Index));
                }
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"User '{user.FullName}' {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: User/ResetPassword/id
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.UserName = user.FullName;
            return View(new ResetPasswordViewModel { UserId = id });
        }

        // POST: User/ResetPassword
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var user2 = await _userManager.FindByIdAsync(model.UserId);
                ViewBag.UserName = user2?.FullName;
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);
                ViewBag.UserName = user.FullName;
                return View(model);
            }

            TempData["Success"] = $"Password for '{user.FullName}' has been reset.";
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private async Task<UserViewModel> BuildViewModel(UserViewModel? existing)
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var stores = await _db.Stores.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

            var vm = existing ?? new UserViewModel();
            vm.Roles = roles.Select(r => new SelectListItem(r.Name, r.Name));
            vm.Stores = stores.Select(s => new SelectListItem(s.Name, s.Id.ToString()));
            return vm;
        }
    }
}
