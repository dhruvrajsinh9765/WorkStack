using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class TaskDetailsViewModel
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }

        public int BoardId { get; set; }

        public int ListId { get; set; }

        public string ListName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TaskPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public string CreatorName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool CanManage { get; set; }

        public List<TaskAssigneeViewModel> Assignees { get; set; } = new();

        public List<CommentViewModel> Comments { get; set; } = new();

        public List<TaskWorkspaceMemberViewModel> WorkspaceMembers { get; set; } = new();
    }
}