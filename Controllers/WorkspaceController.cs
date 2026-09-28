using System.ComponentModel.DataAnnotations;
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
    public class WorkspaceController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager) : Controller
    {
        // ============================================================
        // WORKSPACE INDEX
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            search = search?.Trim() ?? string.Empty;

            var workspacesQuery = context.WorkspaceMembers
                .Where(member => member.UserId == userId)
                .Select(member => member.Workspace);

            if (!string.IsNullOrWhiteSpace(search))
            {
                workspacesQuery = workspacesQuery.Where(workspace =>
                    workspace.Name.Contains(search) ||
                    (workspace.Description != null &&
                     workspace.Description.Contains(search)));
            }

            var workspaces = await workspacesQuery
                .AsNoTracking()
                .OrderBy(workspace => workspace.Name)
                .ToListAsync();

            ViewData["Search"] = search;

            return View(workspaces);
        }


        // ============================================================
        // WORKSPACE DETAILS
        // ============================================================

        [HttpGet("/Workspace/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            var membership = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == id &&
                    member.UserId == userId)
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


        // ============================================================
        // EDIT WORKSPACE - GET
        // ============================================================

        [HttpGet("/Workspace/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var authorizationResult =
                await CheckWorkspaceOwnerAccessAsync(id);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var workspace = await context.Workspaces
                .Where(workspace => workspace.Id == id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (workspace is null)
            {
                return NotFound();
            }

            var model = new CreateWorkspaceViewModel
            {
                Name = workspace.Name,
                Description = workspace.Description
            };

            ViewData["WorkspaceId"] = id;

            return View(model);
        }


        // ============================================================
        // EDIT WORKSPACE - POST
        // ============================================================

        [HttpPost("/Workspace/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            CreateWorkspaceViewModel model)
        {
            var authorizationResult =
                await CheckWorkspaceOwnerAccessAsync(id);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var workspace = await context.Workspaces
                .FirstOrDefaultAsync(workspace =>
                    workspace.Id == id);

            if (workspace is null)
            {
                return NotFound();
            }

            // Normalize user input before applying
            // custom validation rules.
            model.Name = (model.Name ?? string.Empty).Trim();

            model.Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim();

            // Remove validation results generated from the
            // untrimmed values and validate the normalized values.
            ModelState.Remove(nameof(model.Name));
            ModelState.Remove(nameof(model.Description));

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Workspace name is required.");
            }
            else if (model.Name.Length > 100)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Workspace name cannot be longer than 100 characters.");
            }

            if (model.Description is not null &&
                model.Description.Length > 500)
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Workspace description cannot be longer than 500 characters.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["WorkspaceId"] = id;

                return View(model);
            }

            workspace.Name = model.Name;

            workspace.Description = model.Description;

            workspace.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }


        // ============================================================
        // DELETE WORKSPACE - POST
        // ============================================================

        [HttpPost("/Workspace/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var authorizationResult =
                await CheckWorkspaceOwnerAccessAsync(id);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var workspace = await context.Workspaces
                .FirstOrDefaultAsync(workspace =>
                    workspace.Id == id);

            if (workspace is null)
            {
                return NotFound();
            }

            // Tasks prevent Lists from being deleted because
            // List -> Task uses DeleteBehavior.Restrict.
            //
            // Therefore, check for Tasks before deleting
            // the Workspace.

            var hasTasks = await context.Tasks
                .AnyAsync(task =>
                    task.List.Board.WorkspaceId == id);

            if (hasTasks)
            {
                TempData["WorkspaceDeleteError"] =
                    "This workspace cannot be deleted while it contains tasks. Remove or move the tasks first.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id
                    });
            }

            context.Workspaces.Remove(workspace);

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // MEMBERS
        // ============================================================

        [HttpGet("/Workspace/Members/{id:int}")]
        public async Task<IActionResult> Members(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            var currentRole = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == id &&
                    member.UserId == userId)
                .AsNoTracking()
                .Select(member =>
                    (WorkspaceRole?)member.Role)
                .FirstOrDefaultAsync();

            if (currentRole is null)
            {
                return NotFound();
            }

            var members = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == id)
                .Select(member => new WorkspaceMemberViewModel
                {
                    UserId = member.UserId,
                    Email = member.User.Email,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,

                    CanBeRemovedByCurrentUser =
                        CanRemoveMember(
                            currentRole.Value,
                            member.Role,
                            userId,
                            member.UserId),

                    CanChangeRole =
                        currentRole == WorkspaceRole.Owner &&
                        member.Role != WorkspaceRole.Owner &&
                        member.UserId != userId
                })
                .AsNoTracking()
                .ToListAsync();

            ViewData["WorkspaceId"] = id;

            ViewData["CanManageMembers"] =
                currentRole is
                    WorkspaceRole.Owner or
                    WorkspaceRole.Manager;

            return View(members);
        }


        // ============================================================
        // CHANGE MEMBER ROLE - GET
        // ============================================================

        [HttpGet("/Workspace/ChangeMemberRole/{workspaceId:int}/{userId}")]
        public async Task<IActionResult> ChangeMemberRole(
            int workspaceId,
            string userId)
        {
            var authorizationResult =
                await CheckWorkspaceOwnerAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var targetMember = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId)
                .Include(member => member.User)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (targetMember is null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (currentUserId == targetMember.UserId ||
                targetMember.Role == WorkspaceRole.Owner)
            {
                return Forbid();
            }

            ViewData["WorkspaceId"] = workspaceId;

            return View(
                new ChangeWorkspaceMemberRoleViewModel
                {
                    Email = targetMember.User.Email,
                    CurrentRole = targetMember.Role,
                    NewRole = targetMember.Role
                });
        }


        // ============================================================
        // CHANGE MEMBER ROLE - POST
        // ============================================================

        [HttpPost("/Workspace/ChangeMemberRole/{workspaceId:int}/{userId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeMemberRole(
            int workspaceId,
            string userId,
            ChangeWorkspaceMemberRoleViewModel model)
        {
            var authorizationResult =
                await CheckWorkspaceOwnerAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var targetMember = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId)
                .Include(member => member.User)
                .FirstOrDefaultAsync();

            if (targetMember is null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (currentUserId == targetMember.UserId ||
                targetMember.Role == WorkspaceRole.Owner)
            {
                return Forbid();
            }

            if (model.NewRole is not
                (WorkspaceRole.Member or WorkspaceRole.Manager))
            {
                ModelState.AddModelError(
                    nameof(model.NewRole),
                    "Select Member or Manager. The Owner role cannot be assigned here.");
            }

            if (!ModelState.IsValid)
            {
                model.Email = targetMember.User.Email;
                model.CurrentRole = targetMember.Role;

                ViewData["WorkspaceId"] = workspaceId;

                return View(model);
            }

            targetMember.Role = model.NewRole;

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Members),
                new
                {
                    id = workspaceId
                });
        }


        // ============================================================
        // REMOVE MEMBER
        // ============================================================

        [HttpPost("/Workspace/RemoveMember/{workspaceId:int}/{userId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(
            int workspaceId,
            string userId)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (currentUserId is null)
            {
                return Forbid();
            }

            var currentRole = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == currentUserId)
                .Select(member =>
                    (WorkspaceRole?)member.Role)
                .FirstOrDefaultAsync();

            if (currentRole is null)
            {
                return NotFound();
            }

            var targetMember = await context.WorkspaceMembers
                .FirstOrDefaultAsync(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId);

            if (targetMember is null)
            {
                return NotFound();
            }

            if (!CanRemoveMember(
                    currentRole.Value,
                    targetMember.Role,
                    currentUserId,
                    targetMember.UserId))
            {
                return Forbid();
            }

            // Do not allow a member to be removed while
            // application data still references that user.
            //
            // This prevents DeleteBehavior.Restrict from
            // producing a database foreign-key exception.

            var hasCreatedTasks = await context.Tasks
                .AnyAsync(task =>
                    task.CreatorId == userId &&
                    task.List.Board.WorkspaceId == workspaceId);

            var hasAssignedTasks = await context.TaskAssignees
                .AnyAsync(assignee =>
                    assignee.UserId == userId &&
                    assignee.Task.List.Board.WorkspaceId == workspaceId);

            var hasComments = await context.Comments
                .AnyAsync(comment =>
                    comment.UserId == userId &&
                    comment.Task.List.Board.WorkspaceId == workspaceId);

            if (hasCreatedTasks ||
                hasAssignedTasks ||
                hasComments)
            {
                TempData["MemberRemoveError"] =
                    "This member cannot be removed while they are referenced by tasks or comments. Remove their task assignments and handle their task/comment data first.";

                return RedirectToAction(
                    nameof(Members),
                    new
                    {
                        id = workspaceId
                    });
            }

            context.WorkspaceMembers.Remove(targetMember);

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Members),
                new
                {
                    id = workspaceId
                });
        }


        // ============================================================
        // ADD MEMBER - GET
        // ============================================================

        [HttpGet("/Workspace/AddMember/{id:int}")]
        public async Task<IActionResult> AddMember(int id)
        {
            var authorizationResult =
                await CheckWorkspaceManagerAccessAsync(id);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            ViewData["WorkspaceId"] = id;

            return View(
                new AddWorkspaceMemberViewModel
                {
                    Role = WorkspaceRole.Member
                });
        }


        // ============================================================
        // ADD MEMBER - POST
        // ============================================================

        [HttpPost("/Workspace/AddMember/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(
            int id,
            AddWorkspaceMemberViewModel model)
        {
            var authorizationResult =
                await CheckWorkspaceManagerAccessAsync(id);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            ViewData["WorkspaceId"] = id;

            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (currentUserId is null)
            {
                return Forbid();
            }

            var currentRole = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == id &&
                    member.UserId == currentUserId)
                .Select(member =>
                    (WorkspaceRole?)member.Role)
                .FirstOrDefaultAsync();

            if (currentRole is null)
            {
                return NotFound();
            }

            // Normalize email before using it for lookup.
            model.Email =
                (model.Email ?? string.Empty).Trim();

            // The DataAnnotations validation may have evaluated
            // the untrimmed value during model binding, so remove
            // the old email validation result and validate the
            // normalized value here.
            ModelState.Remove(nameof(model.Email));

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email address is required.");
            }
            else if (model.Email.Length > 256)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email address cannot be longer than 256 characters.");
            }
            else if (!new EmailAddressAttribute().IsValid(model.Email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Enter a valid email address.");
            }

            // Managers may add Members.
            // Only Owners may add Managers.
            if (model.Role == WorkspaceRole.Manager &&
                currentRole != WorkspaceRole.Owner)
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Only the workspace Owner can add a Manager.");
            }

            if (model.Role is not
                (WorkspaceRole.Member or WorkspaceRole.Manager))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Select Member or Manager. The Owner role cannot be assigned here.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var targetUser =
                await userManager.FindByEmailAsync(model.Email);

            if (targetUser is null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "No registered user was found with that email address.");

                return View(model);
            }

            var alreadyMember =
                await context.WorkspaceMembers
                    .AnyAsync(member =>
                        member.WorkspaceId == id &&
                        member.UserId == targetUser.Id);

            if (alreadyMember)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "That user is already a member of this workspace.");

                return View(model);
            }

            context.WorkspaceMembers.Add(
                new WorkspaceMember
                {
                    WorkspaceId = id,
                    UserId = targetUser.Id,
                    Role = model.Role,
                    JoinedAt = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Members),
                new
                {
                    id
                });
        }


        // ============================================================
        // CREATE WORKSPACE - GET
        // ============================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(
                new CreateWorkspaceViewModel());
        }


        // ============================================================
        // CREATE WORKSPACE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateWorkspaceViewModel model)
        {
            // Normalize input before validation.
            model.Name =
                (model.Name ?? string.Empty).Trim();

            model.Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim();

            // Remove validation results generated from the
            // untrimmed values and validate the normalized values.
            ModelState.Remove(nameof(model.Name));
            ModelState.Remove(nameof(model.Description));

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Workspace name is required.");
            }
            else if (model.Name.Length > 100)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Workspace name cannot be longer than 100 characters.");
            }

            if (model.Description is not null &&
                model.Description.Length > 500)
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Workspace description cannot be longer than 500 characters.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            var workspace = new Workspace
            {
                Name = model.Name,

                Description = model.Description,

                OwnerId = userId,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            workspace.Members.Add(
                new WorkspaceMember
                {
                    UserId = userId,
                    Role = WorkspaceRole.Owner,
                    JoinedAt = DateTime.UtcNow
                });

            context.Workspaces.Add(workspace);

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index));
        }


        // ============================================================
        // WORKSPACE MANAGER ACCESS
        // ============================================================

        private async Task<IActionResult?>
            CheckWorkspaceManagerAccessAsync(
                int workspaceId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            var role = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId)
                .AsNoTracking()
                .Select(member =>
                    (WorkspaceRole?)member.Role)
                .FirstOrDefaultAsync();

            if (role is null)
            {
                return NotFound();
            }

            if (role is not
                (WorkspaceRole.Owner or WorkspaceRole.Manager))
            {
                return Forbid();
            }

            return null;
        }


        // ============================================================
        // WORKSPACE OWNER ACCESS
        // ============================================================

        private async Task<IActionResult?>
            CheckWorkspaceOwnerAccessAsync(
                int workspaceId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            var role = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId)
                .AsNoTracking()
                .Select(member =>
                    (WorkspaceRole?)member.Role)
                .FirstOrDefaultAsync();

            if (role is null)
            {
                return NotFound();
            }

            if (role != WorkspaceRole.Owner)
            {
                return Forbid();
            }

            return null;
        }


        // ============================================================
        // MEMBER REMOVAL PERMISSIONS
        // ============================================================

        private static bool CanRemoveMember(
            WorkspaceRole currentRole,
            WorkspaceRole targetRole,
            string currentUserId,
            string targetUserId)
        {
            if (currentUserId == targetUserId ||
                targetRole == WorkspaceRole.Owner)
            {
                return false;
            }

            return currentRole switch
            {
                WorkspaceRole.Owner =>
                    targetRole is
                        WorkspaceRole.Member or
                        WorkspaceRole.Manager,

                WorkspaceRole.Manager =>
                    targetRole == WorkspaceRole.Member,

                _ => false
            };
        }
    }
}