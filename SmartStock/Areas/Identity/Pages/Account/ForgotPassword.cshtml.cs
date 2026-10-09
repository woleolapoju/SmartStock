using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SmartStock.Models;

namespace SmartStock.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email address.")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Don't reveal that the user does not exist or is not confirmed.
            if (user != null && user.IsActive)
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code, email = user.Email },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Reset your SmartStock password",
                    $"""
                    <p>Hi {HtmlEncoder.Default.Encode(user.FullName)},</p>
                    <p>We received a request to reset your SmartStock password. Click the link below to choose a new one:</p>
                    <p><a href="{HtmlEncoder.Default.Encode(callbackUrl!)}">Reset my password</a></p>
                    <p>If you didn't request this, you can safely ignore this email — your password will not be changed.</p>
                    """);

                _logger.LogInformation("Password reset link generated for {Email}.", Input.Email);
            }

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
