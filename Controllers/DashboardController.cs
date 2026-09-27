using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;
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

            var model = new DashboardViewModel
            {
                DisplayName = User.Identity?.Name,
                Email = User.FindFirstValue(ClaimTypes.Email),
                Workspaces = workspaces,
                WorkspaceCount = workspaces.Count,
                BoardCount = workspaces.Sum(workspace => workspace.BoardCount),
                WorkspaceMembershipCount = workspaces.Sum(workspace => workspace.MemberCount)
            };

            return View(model);
        }
    }
}
