using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WorkStack.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(
            SignInManager<IdentityUser> signInManager,
            ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User logged out.");

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        public async Task<IActionResult> OnGet(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User logged out.");

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }
    }
}