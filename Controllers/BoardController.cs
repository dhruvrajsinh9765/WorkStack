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
    public class BoardController(ApplicationDbContext context) : Controller
    {
        // ============================================================
        // BOARD INDEX
        // ============================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards")]
        public async Task<IActionResult> Index(
            int workspaceId,
            string? search)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership.UserId is null)
            {
                return Forbid();
            }

            if (membership.Role is null)
            {
                return NotFound();
            }

            search = search?.Trim() ?? string.Empty;

            var boardsQuery = context.Boards
                .Where(board => board.WorkspaceId == workspaceId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                boardsQuery = boardsQuery.Where(board =>
                    board.Name.Contains(search) ||
                    (board.Description != null &&
                     board.Description.Contains(search)));
            }

            var boards = await boardsQuery
                .AsNoTracking()
                .OrderBy(board => board.Name)
                .Select(board => new BoardListItemViewModel
                {
                    Id = board.Id,
                    Name = board.Name,
                    Description = board.Description
                })
                .ToListAsync();

            return View(new WorkspaceBoardsViewModel
            {
                WorkspaceId = workspaceId,
                WorkspaceName = membership.WorkspaceName!,
                CanManageBoards = CanManageBoards(membership.Role.Value),
                Search = search,
                Boards = boards
            });
        }


        // ============================================================
        // CREATE BOARD - GET
        // ============================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards/Create")]
        public async Task<IActionResult> Create(int workspaceId)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            ViewData["WorkspaceId"] = workspaceId;

            return View(new BoardFormViewModel());
        }


        // ============================================================
        // CREATE BOARD - POST
        // ============================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int workspaceId,
            BoardFormViewModel model)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            ViewData["WorkspaceId"] = workspaceId;

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
                    "Board name is required.");
            }
            else if (model.Name.Length > 100)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Board name cannot be longer than 100 characters.");
            }

            if (model.Description is not null &&
                model.Description.Length > 500)
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Board description cannot be longer than 500 characters.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var now = DateTime.UtcNow;

            context.Boards.Add(new Board
            {
                WorkspaceId = workspaceId,
                Name = model.Name,
                Description = model.Description,
                CreatedAt = now,
                UpdatedAt = now
            });

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new
                {
                    workspaceId
                });
        }


        // ============================================================
        // BOARD DETAILS
        // ============================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}")]
        public async Task<IActionResult> Details(
            int workspaceId,
            int boardId,
            string? search,
            string? priority)
        {
            var membership =
                await GetCurrentMembershipAsync(workspaceId);

            if (membership.UserId is null)
            {
                return Forbid();
            }

            if (membership.Role is null)
            {
                return NotFound();
            }

            search = search?.Trim() ?? string.Empty;
            priority = priority?.Trim() ?? string.Empty;

            TaskPriority? selectedPriority = null;

            if (!string.IsNullOrWhiteSpace(priority) &&
                Enum.TryParse<TaskPriority>(
                    priority,
                    true,
                    out var parsedPriority))
            {
                selectedPriority = parsedPriority;
            }
            else
            {
                priority = string.Empty;
            }

            var board = await context.Boards
                .Where(board =>
                    board.Id == boardId &&
                    board.WorkspaceId == workspaceId)
                .AsNoTracking()
                .Select(board => new BoardDetailsViewModel
                {
                    Id = board.Id,

                    WorkspaceId = board.WorkspaceId,

                    WorkspaceName = board.Workspace.Name,

                    Name = board.Name,

                    Description = board.Description,

                    CreatedAt = board.CreatedAt,

                    UpdatedAt = board.UpdatedAt,

                    CanManageBoards =
                        CanManageBoards(membership.Role.Value),

                    Search = search,

                    Priority = priority,

                    Lists = board.Lists
                        .OrderBy(list => list.Position)
                        .Select(list => new ListViewModel
                        {
                            Id = list.Id,

                            Name = list.Name,

                            Position = list.Position,

                            // Keep this as the total number of tasks
                            // in the list, regardless of filters.
                            TaskCount = list.Tasks.Count(),

                            Tasks = list.Tasks
                                .Where(task =>
                                    string.IsNullOrWhiteSpace(search) ||
                                    task.Title.Contains(search))
                                .Where(task =>
                                    selectedPriority == null ||
                                    task.Priority == selectedPriority.Value)
                                .OrderBy(task => task.CreatedAt)
                                .Select(task => new TaskCardViewModel
                                {
                                    Id = task.Id,

                                    Title = task.Title,

                                    Priority = task.Priority,

                                    DueDate = task.DueDate,

                                    AssigneeNames = task.Assignees
                                        .Select(assignee =>
                                            assignee.User.UserName
                                            ?? assignee.User.Email
                                            ?? "Unknown")
                                        .ToList()
                                })
                                .ToList()
                        })
                        .ToList(),

                    CanManageLists =
                        CanManageBoards(membership.Role.Value)
                })
                .FirstOrDefaultAsync();

            if (board is null)
            {
                return NotFound();
            }

            return View(board);
        }


        // ============================================================
        // EDIT BOARD - GET
        // ============================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Edit")]
        public async Task<IActionResult> Edit(
            int workspaceId,
            int boardId)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var board = await context.Boards
                .Where(board =>
                    board.Id == boardId &&
                    board.WorkspaceId == workspaceId)
                .AsNoTracking()
                .Select(board => new BoardFormViewModel
                {
                    Name = board.Name,
                    Description = board.Description
                })
                .FirstOrDefaultAsync();

            if (board is null)
            {
                return NotFound();
            }

            ViewData["WorkspaceId"] = workspaceId;

            ViewData["BoardId"] = boardId;

            return View(board);
        }


        // ============================================================
        // EDIT BOARD - POST
        // ============================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int workspaceId,
            int boardId,
            BoardFormViewModel model)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var board = await context.Boards
                .FirstOrDefaultAsync(board =>
                    board.Id == boardId &&
                    board.WorkspaceId == workspaceId);

            if (board is null)
            {
                return NotFound();
            }

            ViewData["WorkspaceId"] = workspaceId;

            ViewData["BoardId"] = boardId;

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
                    "Board name is required.");
            }
            else if (model.Name.Length > 100)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Board name cannot be longer than 100 characters.");
            }

            if (model.Description is not null &&
                model.Description.Length > 500)
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Board description cannot be longer than 500 characters.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            board.Name = model.Name;

            board.Description = model.Description;

            board.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new
                {
                    workspaceId,
                    boardId
                });
        }


        // ============================================================
        // DELETE BOARD - POST
        // ============================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int workspaceId,
            int boardId)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var board = await context.Boards
                .FirstOrDefaultAsync(board =>
                    board.Id == boardId &&
                    board.WorkspaceId == workspaceId);

            if (board is null)
            {
                return NotFound();
            }

            var hasTasks = await context.Tasks
                .AnyAsync(task =>
                    task.List.BoardId == boardId);

            if (hasTasks)
            {
                TempData["BoardDeleteError"] =
                    "This board cannot be deleted while its lists contain tasks. Move or remove the tasks first.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        workspaceId,
                        boardId
                    });
            }

            context.Boards.Remove(board);

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new
                {
                    workspaceId
                });
        }


        // ============================================================
        // GET CURRENT WORKSPACE MEMBERSHIP
        // ============================================================

        private async Task<(
            string? UserId,
            WorkspaceRole? Role,
            string? WorkspaceName)> GetCurrentMembershipAsync(
                int workspaceId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return (null, null, null);
            }

            var membership = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId)
                .AsNoTracking()
                .Select(member => new
                {
                    member.Role,
                    WorkspaceName = member.Workspace.Name
                })
                .FirstOrDefaultAsync();

            return (
                userId,
                membership?.Role,
                membership?.WorkspaceName);
        }


        // ============================================================
        // CHECK MANAGEMENT ACCESS
        // ============================================================

        private async Task<IActionResult?> CheckManageAccessAsync(
            int workspaceId)
        {
            var membership =
                await GetCurrentMembershipAsync(workspaceId);

            if (membership.UserId is null)
            {
                return Forbid();
            }

            if (membership.Role is null)
            {
                return NotFound();
            }

            return CanManageBoards(membership.Role.Value)
                ? null
                : Forbid();
        }


        // ============================================================
        // CHECK BOARD MANAGEMENT ROLE
        // ============================================================

        private static bool CanManageBoards(
            WorkspaceRole role)
        {
            return role is
                WorkspaceRole.Owner or
                WorkspaceRole.Manager;
        }
    }
}