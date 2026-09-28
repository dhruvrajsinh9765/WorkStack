using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace WorkStack.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserStore<IdentityUser> _userStore;
    private readonly IUserEmailStore<IdentityUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        IUserStore<IdentityUser> userStore,
        SignInManager<IdentityUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        [Required]
        [StringLength(
            100,
            ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = default!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(
            "Password",
            ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        ExternalLogins =
            (await _signInManager.GetExternalAuthenticationSchemesAsync())
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        ExternalLogins =
            (await _signInManager.GetExternalAuthenticationSchemesAsync())
            .ToList();

        if (ModelState.IsValid)
        {
            var email = Input.Email.Trim();

            var user = CreateUser();

            /*
             * IdentityUser requires a UserName.
             *
             * The user does NOT choose the username during registration.
             * We temporarily use the email as the internal Identity username.
             * The user can choose/change the visible username later
             * from Account Settings.
             */
            await _userStore.SetUserNameAsync(
                user,
                email,
                CancellationToken.None);

            await _emailStore.SetEmailAsync(
                user,
                email,
                CancellationToken.None);

            var result = await _userManager.CreateAsync(
                user,
                Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "User created a new account with password.");

                var userId = await _userManager.GetUserIdAsync(user);

                var code =
                    await _userManager.GenerateEmailConfirmationTokenAsync(user);

                code = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new
                    {
                        area = "Identity",
                        userId,
                        code,
                        returnUrl
                    },
                    protocol: Request.Scheme)!;

                await _emailSender.SendEmailAsync(
                    email,
                    "Confirm your email",
                    $"Please confirm your account by " +
                    $"<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>" +
                    $"clicking here</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage(
                        "RegisterConfirmation",
                        new
                        {
                            email,
                            returnUrl
                        });
                }

                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false);

                /*
                 * After registration, send the user to Profile so they
                 * can choose their WorkStack username.
                 */
                return RedirectToPage(
                    "/Account/Manage/Index",
                    new
                    {
                        area = "Identity"
                    });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }
        }

        return Page();
    }

    private IdentityUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<IdentityUser>();
        }
        catch
        {
            throw new InvalidOperationException(
                $"Can't create an instance of '{nameof(IdentityUser)}'. " +
                $"Ensure that '{nameof(IdentityUser)}' is not an abstract " +
                $"class and has a parameterless constructor.");
        }
    }

    private IUserEmailStore<IdentityUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException(
                "The default UI requires a user store with email support.");
        }

        return (IUserEmailStore<IdentityUser>)_userStore;
    }
}