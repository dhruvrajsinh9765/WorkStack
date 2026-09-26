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
        [HttpGet("/Workspace/{workspaceId:int}/Boards")]
        public async Task<IActionResult> Index(int workspaceId)
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

            var boards = await context.Boards
                .Where(board => board.WorkspaceId == workspaceId)
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
                Boards = boards
            });
        }

        [HttpGet("/Workspace/{workspaceId:int}/Boards/Create")]
        public async Task<IActionResult> Create(int workspaceId)
        {
            var authorizationResult = await CheckManageAccessAsync(workspaceId);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            ViewData["WorkspaceId"] = workspaceId;
            return View(new BoardFormViewModel());
        }

        [HttpPost("/Workspace/{workspaceId:int}/Boards/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int workspaceId, BoardFormViewModel model)
        {
            var authorizationResult = await CheckManageAccessAsync(workspaceId);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            ViewData["WorkspaceId"] = workspaceId;
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
            return RedirectToAction(nameof(Index), new { workspaceId });
        }

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}")]
        public async Task<IActionResult> Details(int workspaceId, int boardId)
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

            var board = await context.Boards
                .Where(board => board.Id == boardId && board.WorkspaceId == workspaceId)
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
                    CanManageBoards = CanManageBoards(membership.Role.Value)
                })
                .FirstOrDefaultAsync();

            if (board is null)
            {
                return NotFound();
            }

            return View(board);
        }

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Edit")]
        public async Task<IActionResult> Edit(int workspaceId, int boardId)
        {
            var authorizationResult = await CheckManageAccessAsync(workspaceId);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var board = await context.Boards
                .Where(board => board.Id == boardId && board.WorkspaceId == workspaceId)
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

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int workspaceId, int boardId, BoardFormViewModel model)
        {
            var authorizationResult = await CheckManageAccessAsync(workspaceId);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var board = await context.Boards
                .FirstOrDefaultAsync(board => board.Id == boardId && board.WorkspaceId == workspaceId);

            if (board is null)
            {
                return NotFound();
            }

            ViewData["WorkspaceId"] = workspaceId;
            ViewData["BoardId"] = boardId;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            board.Name = model.Name;
            board.Description = model.Description;
            board.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { workspaceId, boardId });
        }

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int workspaceId, int boardId)
        {
            var authorizationResult = await CheckManageAccessAsync(workspaceId);
            if (authorizationResult is not null)
            {
                return authorizationResult;
            }

            var board = await context.Boards
                .FirstOrDefaultAsync(board => board.Id == boardId && board.WorkspaceId == workspaceId);

            if (board is null)
            {
                return NotFound();
            }

            var hasTasks = await context.Tasks
                .AnyAsync(task => task.List.BoardId == boardId);
            if (hasTasks)
            {
                TempData["BoardDeleteError"] = "This board cannot be deleted while its lists contain tasks. Move or remove the tasks first.";
                return RedirectToAction(nameof(Details), new { workspaceId, boardId });
            }

            context.Boards.Remove(board);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { workspaceId });
        }

        private async Task<(string? UserId, WorkspaceRole? Role, string? WorkspaceName)> GetCurrentMembershipAsync(int workspaceId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return (null, null, null);
            }

            var membership = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == workspaceId && member.UserId == userId)
                .AsNoTracking()
                .Select(member => new { member.Role, WorkspaceName = member.Workspace.Name })
                .FirstOrDefaultAsync();

            return (userId, membership?.Role, membership?.WorkspaceName);
        }

        private async Task<IActionResult?> CheckManageAccessAsync(int workspaceId)
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

            return CanManageBoards(membership.Role.Value) ? null : Forbid();
        }

        private static bool CanManageBoards(WorkspaceRole role)
        {
            return role is WorkspaceRole.Owner or WorkspaceRole.Manager;
        }
    }
}
