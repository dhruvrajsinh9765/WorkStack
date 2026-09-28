using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;
using WorkStack.Models.Enums;
using WorkStack.Models.ViewModels;

namespace WorkStack.Controllers
{
    [Authorize]
    public class DashboardController(ApplicationDbContext context) : Controller
    {
        [HttpGet("/Dashboard")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            var now = DateTime.UtcNow;
            var dueSoonLimit = now.AddDays(7);

            // Workspaces the current user belongs to.
            var workspaces = await context.WorkspaceMembers
                .Where(member => member.UserId == userId)
                .AsNoTracking()
                .Select(member => new DashboardWorkspaceViewModel
                {
                    WorkspaceId = member.WorkspaceId,
                    WorkspaceName = member.Workspace.Name,
                    Description = member.Workspace.Description,
                    Role = member.Role,
                    MemberCount = member.Workspace.Members.Count(),
                    BoardCount = member.Workspace.Boards.Count()
                })
                .ToListAsync();

            // Tasks assigned to the current user.
            var myTasks = await context.TaskAssignees
                .Where(assignee => assignee.UserId == userId)
                .AsNoTracking()
                .Select(assignee => new DashboardTaskViewModel
                {
                    TaskId = assignee.TaskId,
                    WorkspaceId = assignee.Task.List.Board.WorkspaceId,
                    BoardId = assignee.Task.List.BoardId,
                    ListId = assignee.Task.ListId,
                    Title = assignee.Task.Title,
                    WorkspaceName = assignee.Task.List.Board.Workspace.Name,
                    BoardName = assignee.Task.List.Board.Name,
                    ListName = assignee.Task.List.Name,
                    Priority = assignee.Task.Priority,
                    DueDate = assignee.Task.DueDate
                })
                .OrderBy(task => task.DueDate.HasValue ? 0 : 1)
                .ThenBy(task => task.DueDate)
                .ThenBy(task => task.Priority)
                .ThenBy(task => task.Title)
                .Take(10)
                .ToListAsync();

            // Calculate overdue status after the database query.
            foreach (var task in myTasks)
            {
                task.IsOverdue = task.DueDate.HasValue
                    && task.DueDate.Value < now;
            }

            // Counts are calculated from the complete assigned-task query.
            var assignedTaskQuery = context.TaskAssignees
                .Where(assignee => assignee.UserId == userId)
                .Select(assignee => assignee.Task);

            var myTaskCount = await assignedTaskQuery.CountAsync();

            var dueSoonTaskCount = await assignedTaskQuery
                .CountAsync(task =>
                    task.DueDate.HasValue
                    && task.DueDate.Value >= now
                    && task.DueDate.Value <= dueSoonLimit);

            var urgentTaskCount = await assignedTaskQuery
                .CountAsync(task => task.Priority == TaskPriority.Urgent);

            var model = new DashboardViewModel
            {
                DisplayName = User.Identity?.Name,
                Email = User.FindFirstValue(ClaimTypes.Email),

                Workspaces = workspaces,

                WorkspaceCount = workspaces.Count,

                BoardCount = workspaces.Sum(workspace => workspace.BoardCount),

                WorkspaceMembershipCount = workspaces.Sum(
                    workspace => workspace.MemberCount),

                MyTaskCount = myTaskCount,

                DueSoonTaskCount = dueSoonTaskCount,

                UrgentTaskCount = urgentTaskCount,

                MyTasks = myTasks
            };

            return View(model);
        }
    }
}