using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;
using WorkStack.Models;
using WorkStack.Models.Enums;

namespace WorkStack.Controllers
{
    [Authorize]
    public class ListController(ApplicationDbContext context) : Controller
    {
        // ============================================================
        // CREATE LIST - GET
        // ============================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/Create")]
        public async Task<IActionResult> Create(
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
                .FirstOrDefaultAsync();

            if (board is null)
            {
                return NotFound();
            }

            ViewData["WorkspaceId"] = workspaceId;
            ViewData["BoardId"] = boardId;
            ViewData["BoardName"] = board.Name;

            return View();
        }


        // ============================================================
        // CREATE LIST - POST
        // ============================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int workspaceId,
            int boardId,
            string name)
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
                .FirstOrDefaultAsync();

            if (board is null)
            {
                return NotFound();
            }

            name = name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(
                    "name",
                    "List name is required.");
            }
            else if (name.Length > 100)
            {
                ModelState.AddModelError(
                    "name",
                    "List name cannot be longer than 100 characters.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["WorkspaceId"] = workspaceId;
                ViewData["BoardId"] = boardId;
                ViewData["BoardName"] = board.Name;

                return View();
            }

            var maxPosition = await context.Lists
                .Where(list => list.BoardId == boardId)
                .Select(list => (int?)list.Position)
                .MaxAsync();

            var nextPosition = (maxPosition ?? 0) + 1;

            var now = DateTime.UtcNow;

            var list = new WorkStack.Models.List
            {
                Name = name,
                Position = nextPosition,
                BoardId = boardId,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Lists.Add(list);

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(BoardController.Details),
                "Board",
                new
                {
                    workspaceId,
                    boardId
                });
        }


        // ============================================================
        // EDIT LIST - GET
        // ============================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Edit")]
        public async Task<IActionResult> Edit(
            int workspaceId,
            int boardId,
            int listId)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var list = await context.Lists
                .Include(list => list.Board)
                .Where(list =>
                    list.Id == listId &&
                    list.BoardId == boardId &&
                    list.Board.WorkspaceId == workspaceId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (list is null)
            {
                return NotFound();
            }

            ViewData["WorkspaceId"] = workspaceId;
            ViewData["BoardId"] = boardId;
            ViewData["BoardName"] = list.Board.Name;

            return View(list);
        }


        // ============================================================
        // EDIT LIST - POST
        // ============================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int workspaceId,
            int boardId,
            int listId,
            string name)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var list = await context.Lists
                .Include(list => list.Board)
                .Where(list =>
                    list.Id == listId &&
                    list.BoardId == boardId &&
                    list.Board.WorkspaceId == workspaceId)
                .FirstOrDefaultAsync();

            if (list is null)
            {
                return NotFound();
            }

            name = name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(
                    "name",
                    "List name is required.");
            }
            else if (name.Length > 100)
            {
                ModelState.AddModelError(
                    "name",
                    "List name cannot be longer than 100 characters.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["WorkspaceId"] = workspaceId;
                ViewData["BoardId"] = boardId;
                ViewData["BoardName"] = list.Board.Name;

                return View(list);
            }

            list.Name = name;
            list.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(BoardController.Details),
                "Board",
                new
                {
                    workspaceId,
                    boardId
                });
        }


        // ============================================================
        // DELETE LIST - POST
        // ============================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int workspaceId,
            int boardId,
            int listId)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var list = await context.Lists
                .Include(list => list.Board)
                .Where(list =>
                    list.Id == listId &&
                    list.BoardId == boardId &&
                    list.Board.WorkspaceId == workspaceId)
                .FirstOrDefaultAsync();

            if (list is null)
            {
                return NotFound();
            }

            var hasTasks = await context.Tasks
                .AnyAsync(task => task.ListId == listId);

            if (hasTasks)
            {
                TempData["ListDeleteError"] =
                    "This list cannot be deleted while it contains tasks. Move or remove the tasks first.";

                return RedirectToAction(
                    nameof(BoardController.Details),
                    "Board",
                    new
                    {
                        workspaceId,
                        boardId
                    });
            }

            context.Lists.Remove(list);

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(BoardController.Details),
                "Board",
                new
                {
                    workspaceId,
                    boardId
                });
        }


        // ============================================================
        // MOVE LIST UP
        // ============================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/MoveUp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveUp(
            int workspaceId,
            int boardId,
            int listId)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var currentList = await context.Lists
                .Include(list => list.Board)
                .Where(list =>
                    list.Id == listId &&
                    list.BoardId == boardId &&
                    list.Board.WorkspaceId == workspaceId)
                .FirstOrDefaultAsync();

            if (currentList is null)
            {
                return NotFound();
            }

            var previousList = await context.Lists
                .Where(list =>
                    list.BoardId == boardId &&
                    list.Position < currentList.Position)
                .OrderByDescending(list => list.Position)
                .FirstOrDefaultAsync();

            if (previousList is not null)
            {
                var currentPosition = currentList.Position;

                currentList.Position = previousList.Position;
                previousList.Position = currentPosition;

                currentList.UpdatedAt = DateTime.UtcNow;
                previousList.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(BoardController.Details),
                "Board",
                new
                {
                    workspaceId,
                    boardId
                });
        }


        // ============================================================
        // MOVE LIST DOWN
        // ============================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/MoveDown")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveDown(
            int workspaceId,
            int boardId,
            int listId)
        {
            var authorizationResult =
                await CheckManageAccessAsync(workspaceId);

            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var currentList = await context.Lists
                .Include(list => list.Board)
                .Where(list =>
                    list.Id == listId &&
                    list.BoardId == boardId &&
                    list.Board.WorkspaceId == workspaceId)
                .FirstOrDefaultAsync();

            if (currentList is null)
            {
                return NotFound();
            }

            var nextList = await context.Lists
                .Where(list =>
                    list.BoardId == boardId &&
                    list.Position > currentList.Position)
                .OrderBy(list => list.Position)
                .FirstOrDefaultAsync();

            if (nextList is not null)
            {
                var currentPosition = currentList.Position;

                currentList.Position = nextList.Position;
                nextList.Position = currentPosition;

                currentList.UpdatedAt = DateTime.UtcNow;
                nextList.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(BoardController.Details),
                "Board",
                new
                {
                    workspaceId,
                    boardId
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

            return CanManageLists(membership.Role.Value)
                ? null
                : Forbid();
        }


        // ============================================================
        // CHECK LIST MANAGEMENT ROLE
        // ============================================================

        private static bool CanManageLists(
            WorkspaceRole role)
        {
            return role is
                WorkspaceRole.Owner or
                WorkspaceRole.Manager;
        }
    }
}