using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;
using WorkStack.Models.ViewModels;

namespace WorkStack.Controllers
{
    [Authorize]
    public class ProfileController(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context) : Controller
    {
        // ============================================================
        // PROFILE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return NotFound();
            }

            var workspaceCount = await context.WorkspaceMembers
                .AsNoTracking()
                .CountAsync(member => member.UserId == userId);

            var model = new ProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                WorkspaceCount = workspaceCount
            };

            return View(model);
        }
    }
}