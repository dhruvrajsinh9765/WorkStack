using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class DashboardTaskViewModel
    {
        public int TaskId { get; set; }

        public int WorkspaceId { get; set; }

        public int BoardId { get; set; }

        public int ListId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string WorkspaceName { get; set; } = string.Empty;

        public string BoardName { get; set; } = string.Empty;

        public string ListName { get; set; } = string.Empty;

        public TaskPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public bool IsOverdue { get; set; }
    }
}