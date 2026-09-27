using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkStack.Data;
using WorkStack.Models.ViewModels;

namespace WorkStack.Components
{
    public class WorkspaceNavigationViewComponent(ApplicationDbContext context) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return View(Array.Empty<WorkspaceNavigationItemViewModel>());
            }

            var workspaces = await context.WorkspaceMembers
                .Where(member => member.UserId == userId)
                .AsNoTracking()
                .OrderBy(member => member.Workspace.Name)
                .Select(member => new WorkspaceNavigationItemViewModel
                {
                    Id = member.WorkspaceId,
                    Name = member.Workspace.Name
                })
                .ToListAsync();

            return View(workspaces);
        }
    }
}
