using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class DashboardWorkspaceViewModel
    {
        public int WorkspaceId { get; set; }

        public string WorkspaceName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public WorkspaceRole Role { get; set; }

        public int MemberCount { get; set; }

        public int BoardCount { get; set; }
    }
}