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
        256,
        MinimumLength = 3,
        ErrorMessage = "Username must be between 3 and 256 characters.")]
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

        Username = await _userManager.GetUserNameAsync(user)
            ?? string.Empty;

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

        var currentUsername = await _userManager.GetUserNameAsync(user);

        if (!string.Equals(
                Username,
                currentUsername,
                StringComparison.Ordinal))
        {
            var existingUser = await _userManager.FindByNameAsync(Username);

            if (existingUser is not null &&
                existingUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(Username),
                    "That username is already in use.");

                return Page();
            }

            var setUsernameResult =
                await _userManager.SetUserNameAsync(user, Username);

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
        }

        await _signInManager.RefreshSignInAsync(user);

        StatusMessage = "Your profile has been updated.";

        return RedirectToPage();
    }
}