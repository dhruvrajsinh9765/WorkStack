using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WorkStack.Areas.Identity.Pages.Account
{
    public class LoginModel(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        ILogger<LoginModel> logger) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
            = new List<AuthenticationScheme>();

        public string ReturnUrl { get; set; } = "/Dashboard";

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            ExternalLogins =
                (await signInManager
                    .GetExternalAuthenticationSchemesAsync())
                .ToList();

            ReturnUrl = GetSafeReturnUrl(returnUrl);
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = GetSafeReturnUrl(returnUrl);

            ExternalLogins =
                (await signInManager
                    .GetExternalAuthenticationSchemesAsync())
                .ToList();

            if (ModelState.IsValid)
            {
                // Find the Identity user using the email address.
                var user = await userManager.FindByEmailAsync(
                    Input.Email.Trim());

                if (user is not null &&
                    !string.IsNullOrEmpty(user.UserName))
                {
                    // Sign in using the user's actual Identity username.
                    var result =
                        await signInManager.PasswordSignInAsync(
                            user.UserName,
                            Input.Password,
                            Input.RememberMe,
                            lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        logger.LogInformation(
                            "User logged in.");

                        return LocalRedirect(ReturnUrl);
                    }

                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage(
                            "./LoginWith2fa",
                            new
                            {
                                ReturnUrl,
                                Input.RememberMe
                            });
                    }

                    if (result.IsLockedOut)
                    {
                        logger.LogWarning(
                            "User account locked out.");

                        return RedirectToPage("./Lockout");
                    }
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Invalid login attempt.");
            }

            return Page();
        }

        private string GetSafeReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrEmpty(returnUrl) &&
                   Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Content("~/Dashboard")!;
        }
    }
}