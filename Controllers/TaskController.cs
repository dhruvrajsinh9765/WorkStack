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
    public class TaskController(ApplicationDbContext context) : Controller
    {
        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/Create")]
        public async Task<IActionResult> Create(
            int workspaceId,
            int boardId,
            int listId)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            if (!CanManageTasks(membership.Role))
                return Forbid();

            var list = await context.Lists
                .Include(item => item.Board)
                .FirstOrDefaultAsync(item =>
                    item.Id == listId &&
                    item.BoardId == boardId &&
                    item.Board.WorkspaceId == workspaceId);

            if (list is null)
                return NotFound();

            var workspaceMembers = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == workspaceId)
                .Include(member => member.User)
                .AsNoTracking()
                .OrderBy(member => member.User.UserName)
                .Select(member => new TaskWorkspaceMemberViewModel
                {
                    UserId = member.UserId,
                    UserName = member.User.UserName
                        ?? member.User.Email
                        ?? "Unknown",
                    IsAssigned = false
                })
                .ToListAsync();

            var model = new CreateTaskViewModel
            {
                WorkspaceId = workspaceId,
                BoardId = boardId,
                ListId = listId,
                ListName = list.Name,
                Priority = TaskPriority.Medium,
                WorkspaceMembers = workspaceMembers
            };

            return View(model);
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int workspaceId,
            int boardId,
            int listId,
            CreateTaskViewModel model)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            if (!CanManageTasks(membership.Role))
                return Forbid();

            var list = await context.Lists
                .Include(item => item.Board)
                .FirstOrDefaultAsync(item =>
                    item.Id == listId &&
                    item.BoardId == boardId &&
                    item.Board.WorkspaceId == workspaceId);

            if (list is null)
                return NotFound();

            model.WorkspaceId = workspaceId;
            model.BoardId = boardId;
            model.ListId = listId;
            model.ListName = list.Name;

            model.AssigneeIds ??= new List<string>();

            // Normalize task input before validation.
            model.Title =
                (model.Title ?? string.Empty).Trim();

            model.Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim();

            // Remove validation results generated from the
            // untrimmed values and validate the normalized values.
            ModelState.Remove(nameof(model.Title));
            ModelState.Remove(nameof(model.Description));

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Task title is required.");
            }
            else if (model.Title.Length > 200)
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Task title cannot be longer than 200 characters.");
            }

            if (model.Description is not null &&
                model.Description.Length > 5000)
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Task description cannot be longer than 5000 characters.");
            }

            // Only allow users who are actual members of this workspace.
            var validMemberIds = await context.WorkspaceMembers
                .Where(member =>
                    member.WorkspaceId == workspaceId)
                .Select(member => member.UserId)
                .ToListAsync();

            var validMemberIdSet =
                validMemberIds.ToHashSet();

            var invalidAssigneeIds = model.AssigneeIds
                .Where(id =>
                    string.IsNullOrWhiteSpace(id) ||
                    !validMemberIdSet.Contains(id))
                .Distinct()
                .ToList();

            if (invalidAssigneeIds.Count > 0)
            {
                ModelState.AddModelError(
                    nameof(model.AssigneeIds),
                    "One or more selected assignees are not members of this workspace.");
            }

            // Reload members if validation fails.
            model.WorkspaceMembers = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == workspaceId)
                .Include(member => member.User)
                .AsNoTracking()
                .OrderBy(member => member.User.UserName)
                .Select(member => new TaskWorkspaceMemberViewModel
                {
                    UserId = member.UserId,
                    UserName = member.User.UserName
                        ?? member.User.Email
                        ?? "Unknown",
                    IsAssigned = model.AssigneeIds.Contains(member.UserId)
                })
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
                return Forbid();

            var task = new WorkStack.Models.Task
            {
                Title = model.Title,

                Description = model.Description,

                ListId = listId,

                CreatorId = userId,

                Priority = model.Priority,

                DueDate = model.DueDate,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            context.Tasks.Add(task);

            await context.SaveChangesAsync();

            // At this point all submitted assignee IDs have
            // already been verified as workspace members.
            if (model.AssigneeIds.Count > 0)
            {
                var uniqueAssigneeIds = model.AssigneeIds
                    .Distinct()
                    .ToList();

                foreach (var memberId in uniqueAssigneeIds)
                {
                    context.TaskAssignees.Add(
                        new TaskAssignee
                        {
                            TaskId = task.Id,
                            UserId = memberId
                        });
                }

                await context.SaveChangesAsync();
            }

            return RedirectToAction(
                "Details",
                "Board",
                new
                {
                    workspaceId,
                    boardId
                });
        }


        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}")]
        public async Task<IActionResult> Details(
            int workspaceId,
            int boardId,
            int listId,
            int taskId)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            var task = await context.Tasks
                .Include(item => item.List)
                    .ThenInclude(item => item.Board)
                .Include(item => item.Creator)
                .Include(item => item.Assignees)
                    .ThenInclude(assignee => assignee.User)
                .Include(item => item.Comments)
                    .ThenInclude(comment => comment.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == taskId &&
                    item.ListId == listId &&
                    item.List.BoardId == boardId &&
                    item.List.Board.WorkspaceId == workspaceId);

            if (task is null)
                return NotFound();

            var workspaceMembers = await context.WorkspaceMembers
                .Where(member => member.WorkspaceId == workspaceId)
                .Include(member => member.User)
                .AsNoTracking()
                .OrderBy(member => member.User.UserName)
                .ToListAsync();

            var assignedUserIds = task.Assignees
                .Select(assignee => assignee.UserId)
                .ToHashSet();

            var model = new TaskDetailsViewModel
            {
                Id = task.Id,

                WorkspaceId = workspaceId,

                BoardId = boardId,

                ListId = listId,

                ListName = task.List.Name,

                Title = task.Title,

                Description = task.Description,

                Priority = task.Priority,

                DueDate = task.DueDate,

                CreatorName = task.Creator.UserName
                    ?? task.Creator.Email
                    ?? "Unknown",

                CreatedAt = task.CreatedAt,

                UpdatedAt = task.UpdatedAt,

                CanManage = CanManageTasks(membership.Role),

                Assignees = task.Assignees
                    .Select(assignee => new TaskAssigneeViewModel
                    {
                        UserId = assignee.UserId,

                        UserName = assignee.User.UserName
                            ?? assignee.User.Email
                            ?? "Unknown"
                    })
                    .ToList(),

                Comments = task.Comments
                    .OrderByDescending(comment => comment.CreatedAt)
                    .Select(comment => new CommentViewModel
                    {
                        Id = comment.Id,

                        UserId = comment.UserId,

                        UserName = comment.User.UserName
                            ?? comment.User.Email
                            ?? "Unknown",

                        Content = comment.Content,

                        CreatedAt = comment.CreatedAt
                    })
                    .ToList(),

                WorkspaceMembers = workspaceMembers
                    .Select(member => new TaskWorkspaceMemberViewModel
                    {
                        UserId = member.UserId,

                        UserName = member.User.UserName
                            ?? member.User.Email
                            ?? "Unknown",

                        IsAssigned = assignedUserIds.Contains(member.UserId)
                    })
                    .ToList()
            };

            return View(model);
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}/Edit")]
        public async Task<IActionResult> Edit(
            int workspaceId,
            int boardId,
            int listId,
            int taskId)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            if (!CanManageTasks(membership.Role))
                return Forbid();

            var task = await context.Tasks
                .Include(item => item.List)
                    .ThenInclude(item => item.Board)
                .FirstOrDefaultAsync(item =>
                    item.Id == taskId &&
                    item.ListId == listId &&
                    item.List.BoardId == boardId &&
                    item.List.Board.WorkspaceId == workspaceId);

            if (task is null)
                return NotFound();

            var model = new EditTaskViewModel
            {
                Id = task.Id,

                WorkspaceId = workspaceId,

                BoardId = boardId,

                ListId = listId,

                Title = task.Title,

                Description = task.Description,

                Priority = task.Priority,

                DueDate = task.DueDate
            };

            return View(model);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int workspaceId,
            int boardId,
            int listId,
            int taskId,
            EditTaskViewModel model)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            if (!CanManageTasks(membership.Role))
                return Forbid();

            var task = await context.Tasks
                .Include(item => item.List)
                    .ThenInclude(item => item.Board)
                .FirstOrDefaultAsync(item =>
                    item.Id == taskId &&
                    item.ListId == listId &&
                    item.List.BoardId == boardId &&
                    item.List.Board.WorkspaceId == workspaceId);

            if (task is null)
                return NotFound();

            // Normalize task input before validation.
            model.Title =
                (model.Title ?? string.Empty).Trim();

            model.Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim();

            // Remove validation results generated from the
            // untrimmed values and validate the normalized values.
            ModelState.Remove(nameof(model.Title));
            ModelState.Remove(nameof(model.Description));

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Task title is required.");
            }
            else if (model.Title.Length > 200)
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Task title cannot be longer than 200 characters.");
            }

            if (model.Description is not null &&
                model.Description.Length > 5000)
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Task description cannot be longer than 5000 characters.");
            }

            if (!ModelState.IsValid)
                return View(model);

            task.Title = model.Title;

            task.Description = model.Description;

            task.Priority = model.Priority;

            task.DueDate = model.DueDate;

            task.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new
                {
                    workspaceId,
                    boardId,
                    listId,
                    taskId
                });
        }


        // =========================================================
        // DELETE
        // =========================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int workspaceId,
            int boardId,
            int listId,
            int taskId)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            if (!CanManageTasks(membership.Role))
                return Forbid();

            var task = await context.Tasks
                .FirstOrDefaultAsync(item =>
                    item.Id == taskId &&
                    item.ListId == listId &&
                    item.List.BoardId == boardId &&
                    item.List.Board.WorkspaceId == workspaceId);

            if (task is null)
                return NotFound();

            context.Tasks.Remove(task);

            await context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Board",
                new
                {
                    workspaceId,
                    boardId
                });
        }


        // =========================================================
        // MOVE TASK
        // =========================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}/Move")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(
            int workspaceId,
            int boardId,
            int listId,
            int taskId,
            int targetListId)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            if (!CanManageTasks(membership.Role))
                return Forbid();

            var task = await context.Tasks
                .Include(item => item.List)
                    .ThenInclude(item => item.Board)
                .FirstOrDefaultAsync(item =>
                    item.Id == taskId &&
                    item.ListId == listId &&
                    item.List.BoardId == boardId &&
                    item.List.Board.WorkspaceId == workspaceId);

            if (task is null)
                return NotFound();

            var targetList = await context.Lists
                .Include(item => item.Board)
                .FirstOrDefaultAsync(item =>
                    item.Id == targetListId &&
                    item.BoardId == boardId &&
                    item.Board.WorkspaceId == workspaceId);

            if (targetList is null)
                return NotFound();

            if (task.ListId != targetList.Id)
            {
                task.ListId = targetList.Id;
                task.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }

            return RedirectToAction(
                "Details",
                "Board",
                new
                {
                    workspaceId,
                    boardId
                });
        }


        // =========================================================
        // ASSIGN
        // =========================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}/Assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(
            int workspaceId,
            int boardId,
            int listId,
            int taskId,
            string userId)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            if (!CanManageTasks(membership.Role))
                return Forbid();

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["TaskAssignmentError"] =
                    "A valid workspace member must be selected.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        workspaceId,
                        boardId,
                        listId,
                        taskId
                    });
            }

            userId = userId.Trim();

            var taskExists = await context.Tasks
                .AnyAsync(item =>
                    item.Id == taskId &&
                    item.ListId == listId &&
                    item.List.BoardId == boardId &&
                    item.List.Board.WorkspaceId == workspaceId);

            if (!taskExists)
                return NotFound();

            var isWorkspaceMember = await context.WorkspaceMembers
                .AnyAsync(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId);

            if (!isWorkspaceMember)
            {
                TempData["TaskAssignmentError"] =
                    "The selected user is not a member of this workspace.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        workspaceId,
                        boardId,
                        listId,
                        taskId
                    });
            }

            var alreadyAssigned = await context.TaskAssignees
                .AnyAsync(assignee =>
                    assignee.TaskId == taskId &&
                    assignee.UserId == userId);

            if (!alreadyAssigned)
            {
                context.TaskAssignees.Add(
                    new TaskAssignee
                    {
                        TaskId = taskId,
                        UserId = userId
                    });

                await context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(Details),
                new
                {
                    workspaceId,
                    boardId,
                    listId,
                    taskId
                });
        }


        // =========================================================
        // UNASSIGN
        // =========================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}/Unassign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unassign(
            int workspaceId,
            int boardId,
            int listId,
            int taskId,
            string userId)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            if (!CanManageTasks(membership.Role))
                return Forbid();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest();
            }

            userId = userId.Trim();

            var assignment = await context.TaskAssignees
                .FirstOrDefaultAsync(assignee =>
                    assignee.TaskId == taskId &&
                    assignee.UserId == userId &&
                    assignee.Task.ListId == listId &&
                    assignee.Task.List.BoardId == boardId &&
                    assignee.Task.List.Board.WorkspaceId == workspaceId);

            if (assignment is null)
                return NotFound();

            context.TaskAssignees.Remove(assignment);

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new
                {
                    workspaceId,
                    boardId,
                    listId,
                    taskId
                });
        }


        // =========================================================
        // ADD COMMENT
        // =========================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}/Comments/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(
            int workspaceId,
            int boardId,
            int listId,
            int taskId,
            string content)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            var taskExists = await context.Tasks
                .AnyAsync(item =>
                    item.Id == taskId &&
                    item.ListId == listId &&
                    item.List.BoardId == boardId &&
                    item.List.Board.WorkspaceId == workspaceId);

            if (!taskExists)
                return NotFound();

            var trimmedContent =
                content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedContent))
            {
                TempData["CommentError"] =
                    "Comment cannot be empty.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        workspaceId,
                        boardId,
                        listId,
                        taskId
                    });
            }

            if (trimmedContent.Length > 2000)
            {
                TempData["CommentError"] =
                    "Comment cannot be longer than 2000 characters.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        workspaceId,
                        boardId,
                        listId,
                        taskId
                    });
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
                return Forbid();

            context.Comments.Add(
                new Comment
                {
                    TaskId = taskId,
                    UserId = userId,
                    Content = trimmedContent,
                    CreatedAt = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new
                {
                    workspaceId,
                    boardId,
                    listId,
                    taskId
                });
        }


        // =========================================================
        // DELETE COMMENT
        // =========================================================

        [HttpPost("/Workspace/{workspaceId:int}/Boards/{boardId:int}/Lists/{listId:int}/Tasks/{taskId:int}/Comments/{commentId:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(
            int workspaceId,
            int boardId,
            int listId,
            int taskId,
            int commentId)
        {
            var membership = await GetCurrentMembershipAsync(workspaceId);

            if (membership is null)
                return NotFound();

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
                return Forbid();

            var comment = await context.Comments
                .FirstOrDefaultAsync(item =>
                    item.Id == commentId &&
                    item.TaskId == taskId &&
                    item.UserId == userId &&
                    item.Task.ListId == listId &&
                    item.Task.List.BoardId == boardId &&
                    item.Task.List.Board.WorkspaceId == workspaceId);

            if (comment is null)
                return NotFound();

            context.Comments.Remove(comment);

            await context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new
                {
                    workspaceId,
                    boardId,
                    listId,
                    taskId
                });
        }


        // =========================================================
        // HELPERS
        // =========================================================

        private async Task<WorkspaceMember?> GetCurrentMembershipAsync(
            int workspaceId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (userId is null)
                return null;

            return await context.WorkspaceMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(member =>
                    member.WorkspaceId == workspaceId &&
                    member.UserId == userId);
        }

        private static bool CanManageTasks(
            WorkspaceRole role)
        {
            return role == WorkspaceRole.Owner ||
                   role == WorkspaceRole.Manager;
        }
    }
}