using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;
using WorkStack.Models;
using WorkStack.Models.Enums;
using WorkStack.Models.ViewModels;

namespace WorkStack.Controllers
{
    [Authorize]
    public class WorkspaceController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return Forbid();
            }

            var workspaces = await context.WorkspaceMembers
                .Where(member => member.UserId == userId)
                .Select(member => member.Workspace)
                .AsNoTracking()
                .ToListAsync();

            return View(workspaces);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateWorkspaceViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateWorkspaceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return Forbid();
            }

            var workspace = new Workspace
            {
                Name = model.Name,
                Description = model.Description,
                OwnerId = userId
            };

            workspace.Members.Add(new WorkspaceMember
            {
                UserId = userId,
                Role = WorkspaceRole.Owner
            });

            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
