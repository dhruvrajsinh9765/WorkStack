namespace WorkStack.Models.ViewModels
{
    public class DashboardViewModel
    {
        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public int WorkspaceCount { get; set; }

        public int BoardCount { get; set; }

        public int WorkspaceMembershipCount { get; set; }

        public IReadOnlyList<DashboardWorkspaceViewModel> Workspaces { get; set; } = Array.Empty<DashboardWorkspaceViewModel>();
    }
}
