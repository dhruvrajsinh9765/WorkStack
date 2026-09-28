using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WorkStack.Areas.Identity.Pages.Account.Manage;

public class EmailModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public EmailModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string Email { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "New email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "New email")]
        public string NewEmail { get; set; } = string.Empty;
    }

    private async Task LoadAsync(IdentityUser user)
    {
        Email = await _userManager.GetEmailAsync(user)
            ?? string.Empty;

        Input = new InputModel
        {
            NewEmail = Email
        };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return NotFound(
                $"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);

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

        Input.NewEmail =
            (Input.NewEmail ?? string.Empty).Trim();

        ModelState.Remove(nameof(Input.NewEmail));

        if (string.IsNullOrWhiteSpace(Input.NewEmail))
        {
            ModelState.AddModelError(
                nameof(Input.NewEmail),
                "New email is required.");
        }
        else if (!new EmailAddressAttribute()
            .IsValid(Input.NewEmail))
        {
            ModelState.AddModelError(
                nameof(Input.NewEmail),
                "Please enter a valid email address.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);

            Input.NewEmail =
                Input.NewEmail.Trim();

            return Page();
        }

        var currentEmail =
            await _userManager.GetEmailAsync(user);

        if (string.Equals(
                currentEmail,
                Input.NewEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Your email is unchanged.";

            return RedirectToPage();
        }

        var existingUser =
            await _userManager.FindByEmailAsync(Input.NewEmail);

        if (existingUser is not null &&
            existingUser.Id != user.Id)
        {
            ModelState.AddModelError(
                nameof(Input.NewEmail),
                "That email address is already in use.");

            await LoadAsync(user);

            Input.NewEmail =
                Input.NewEmail.Trim();

            return Page();
        }

        var setEmailResult =
            await _userManager.SetEmailAsync(
                user,
                Input.NewEmail);

        if (!setEmailResult.Succeeded)
        {
            foreach (var error in setEmailResult.Errors)
            {
                ModelState.AddModelError(
                    nameof(Input.NewEmail),
                    error.Description);
            }

            await LoadAsync(user);

            Input.NewEmail =
                Input.NewEmail.Trim();

            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);

        StatusMessage =
            "Your email has been updated successfully.";

        return RedirectToPage();
    }
}