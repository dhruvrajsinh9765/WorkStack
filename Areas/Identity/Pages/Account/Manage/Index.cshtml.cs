using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WorkStack.Areas.Identity.Pages.Account.Manage;

public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public IndexModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(
        50,
        MinimumLength = 3,
        ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression(
        @"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username can contain only letters, numbers, dots, underscores, and hyphens.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return NotFound(
                $"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var currentUsername =
            await _userManager.GetUserNameAsync(user);

        /*
         * During registration, the email is temporarily stored as
         * Identity's UserName because Identity requires a username.
         *
         * Do not show that email as the user's WorkStack username.
         * Instead, leave the field empty so the user can choose one.
         */
        if (!string.IsNullOrWhiteSpace(currentUsername) &&
            !string.Equals(
                currentUsername,
                user.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            Username = currentUsername;
        }
        else
        {
            Username = string.Empty;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return NotFound(
                $"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        Username = Username.Trim();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        /*
         * The visible WorkStack username should be different from
         * the account email.
         */
        if (!string.IsNullOrWhiteSpace(user.Email) &&
            string.Equals(
                Username,
                user.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(Username),
                "Username must be different from your email address.");

            return Page();
        }

        var currentUsername =
            await _userManager.GetUserNameAsync(user);

        /*
         * If the username has not changed, there is nothing to update.
         */
        if (string.Equals(
                Username,
                currentUsername,
                StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Your profile is already up to date.";

            await _signInManager.RefreshSignInAsync(user);

            return RedirectToPage();
        }

        /*
         * Make sure another account is not already using the username.
         */
        var existingUser =
            await _userManager.FindByNameAsync(Username);

        if (existingUser is not null &&
            existingUser.Id != user.Id)
        {
            ModelState.AddModelError(
                nameof(Username),
                "That username is already in use.");

            return Page();
        }

        /*
         * Update Identity username.
         */
        var setUsernameResult =
            await _userManager.SetUserNameAsync(
                user,
                Username);

        if (!setUsernameResult.Succeeded)
        {
            foreach (var error in setUsernameResult.Errors)
            {
                ModelState.AddModelError(
                    nameof(Username),
                    error.Description);
            }

            return Page();
        }

        /*
         * Refresh the authentication cookie so User.Identity.Name
         * immediately contains the new username.
         */
        await _signInManager.RefreshSignInAsync(user);

        StatusMessage = "Your username has been updated successfully.";

        return RedirectToPage();
    }
}