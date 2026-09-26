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

        [HttpGet("/Workspace/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return Forbid();
            }

            var membership = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == id && member.UserId == userId)
                .Select(member => new
                {
                    Workspace = member.Workspace,
                    member.Role
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (membership is null)
            {
                return NotFound();
            }

            var model = new WorkspaceDetailsViewModel
            {
                Id = membership.Workspace.Id,
                Name = membership.Workspace.Name,
                Description = membership.Workspace.Description,
                Role = membership.Role
            };

            return View(model);
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
