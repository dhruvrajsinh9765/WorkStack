namespace WorkStack.Models.ViewModels
{
    public class DashboardViewModel
    {
        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public int WorkspaceCount { get; set; }

        public int BoardCount { get; set; }

        public int WorkspaceMembershipCount { get; set; }

        public int MyTaskCount { get; set; }

        public int DueSoonTaskCount { get; set; }

        public int UrgentTaskCount { get; set; }

        public IReadOnlyList<DashboardWorkspaceViewModel> Workspaces { get; set; }
            = Array.Empty<DashboardWorkspaceViewModel>();

        public IReadOnlyList<DashboardTaskViewModel> MyTasks { get; set; }
            = Array.Empty<DashboardTaskViewModel>();
    }
}   