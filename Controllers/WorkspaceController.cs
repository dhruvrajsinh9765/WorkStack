using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;
using WorkStack.Models;
using WorkStack.Models.Enums;
using WorkStack.Models.ViewModels;

namespace WorkStack.Controllers
{
    [Authorize]
    public class WorkspaceController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : Controller
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

        [HttpGet("/Workspace/Members/{id:int}")]
        public async Task<IActionResult> Members(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return Forbid();
            }

            var currentRole = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == id && member.UserId == userId)
                .AsNoTracking()
                .Select(member => (WorkspaceRole?)member.Role)
                .FirstOrDefaultAsync();

            if (currentRole is null)
            {
                return NotFound();
            }

            var members = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == id)
                .Select(member => new WorkspaceMemberViewModel
                {
                    UserId = member.UserId,
                    Email = member.User.Email,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,
                    CanBeRemovedByCurrentUser = CanRemoveMember(currentRole.Value, member.Role, userId, member.UserId)
                })
                .AsNoTracking()
                .ToListAsync();

            ViewData["WorkspaceId"] = id;
            ViewData["CanManageMembers"] = currentRole is WorkspaceRole.Owner or WorkspaceRole.Manager;
            return View(members);
        }

        [HttpPost("/Workspace/RemoveMember/{workspaceId:int}/{userId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int workspaceId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId is null)
            {
                return Forbid();
            }

            var currentRole = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == workspaceId && member.UserId == currentUserId)
                .Select(member => (WorkspaceRole?)member.Role)
                .FirstOrDefaultAsync();

            if (currentRole is null)
            {
                return NotFound();
            }

            var targetMember = await context.WorkspaceMembers
                .FirstOrDefaultAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId);

            if (targetMember is null)
            {
                return NotFound();
            }

            if (!CanRemoveMember(currentRole.Value, targetMember.Role, currentUserId, targetMember.UserId))
            {
                return Forbid();
            }

            context.WorkspaceMembers.Remove(targetMember);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Members), new { id = workspaceId });
        }

        [HttpGet("/Workspace/AddMember/{id:int}")]
        public async Task<IActionResult> AddMember(int id)
        {
            var authorizationResult = await CheckWorkspaceManagerAccessAsync(id);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            ViewData["WorkspaceId"] = id;
            return View(new AddWorkspaceMemberViewModel { Role = WorkspaceRole.Member });
        }

        [HttpPost("/Workspace/AddMember/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int id, AddWorkspaceMemberViewModel model)
        {
            var authorizationResult = await CheckWorkspaceManagerAccessAsync(id);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            ViewData["WorkspaceId"] = id;

            if (model.Role is not (WorkspaceRole.Member or WorkspaceRole.Manager))
            {
                ModelState.AddModelError(nameof(model.Role), "Select Member or Manager. The Owner role cannot be assigned here.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var targetUser = await userManager.FindByEmailAsync(model.Email);
            if (targetUser is null)
            {
                ModelState.AddModelError(nameof(model.Email), "No registered user was found with that email address.");
                return View(model);
            }

            var alreadyMember = await context.WorkspaceMembers
                .AnyAsync(member => member.WorkspaceId == id && member.UserId == targetUser.Id);
            if (alreadyMember)
            {
                ModelState.AddModelError(nameof(model.Email), "That user is already a member of this workspace.");
                return View(model);
            }

            context.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = id,
                UserId = targetUser.Id,
                Role = model.Role,
                JoinedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Members), new { id });
        }

        private async Task<IActionResult?> CheckWorkspaceManagerAccessAsync(int workspaceId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return Forbid();
            }

            var role = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == workspaceId && member.UserId == userId)
                .AsNoTracking()
                .Select(member => (WorkspaceRole?)member.Role)
                .FirstOrDefaultAsync();

            if (role is null)
            {
                return NotFound();
            }

            if (role is not (WorkspaceRole.Owner or WorkspaceRole.Manager))
            {
                return Forbid();
            }

            return null;
        }

        private static bool CanRemoveMember(WorkspaceRole currentRole, WorkspaceRole targetRole, string currentUserId, string targetUserId)
        {
            if (currentUserId == targetUserId || targetRole == WorkspaceRole.Owner)
            {
                return false;
            }

            return currentRole switch
            {
                WorkspaceRole.Owner => targetRole is WorkspaceRole.Member or WorkspaceRole.Manager,
                WorkspaceRole.Manager => targetRole == WorkspaceRole.Member,
                _ => false
            };
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
