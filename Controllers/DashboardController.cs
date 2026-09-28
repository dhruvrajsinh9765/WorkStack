using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;
using WorkStack.Models.Enums;
using WorkStack.Models.ViewModels;

namespace WorkStack.Controllers
{
    [Authorize]
    public class DashboardController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager) : Controller
    {
        [HttpGet("/Dashboard")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Forbid();
            }

            var currentUser = await userManager.FindByIdAsync(userId);

            if (currentUser is null)
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
                task.IsOverdue =
                    task.DueDate.HasValue &&
                    task.DueDate.Value < now;
            }

            // Counts are calculated from the complete assigned-task query.
            var assignedTaskQuery = context.TaskAssignees
                .Where(assignee => assignee.UserId == userId)
                .Select(assignee => assignee.Task);

            var myTaskCount = await assignedTaskQuery.CountAsync();

            var dueSoonTaskCount = await assignedTaskQuery
                .CountAsync(task =>
                    task.DueDate.HasValue &&
                    task.DueDate.Value >= now &&
                    task.DueDate.Value <= dueSoonLimit);

            var urgentTaskCount = await assignedTaskQuery
                .CountAsync(task =>
                    task.Priority == TaskPriority.Urgent);

            /*
             * Use the WorkStack username for the dashboard.
             *
             * If the user has not chosen a custom username yet,
             * Identity still contains the email as the temporary
             * username. In that case we show "Set your username"
             * instead of exposing the email as the display name.
             */
            var displayName = currentUser.UserName;

            if (string.IsNullOrWhiteSpace(displayName) ||
                string.Equals(
                    displayName,
                    currentUser.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                displayName = "Set your username";
            }

            var model = new DashboardViewModel
            {
                DisplayName = displayName,

                Email = currentUser.Email,

                Workspaces = workspaces,

                WorkspaceCount = workspaces.Count,

                BoardCount = workspaces.Sum(
                    workspace => workspace.BoardCount),

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