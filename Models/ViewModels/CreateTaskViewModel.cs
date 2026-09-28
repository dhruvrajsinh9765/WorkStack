using System.ComponentModel.DataAnnotations;
using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class CreateTaskViewModel
    {
        public int WorkspaceId { get; set; }

        public int BoardId { get; set; }

        public int ListId { get; set; }

        public string ListName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(5000)]
        public string? Description { get; set; }

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        public List<string> AssigneeIds { get; set; } = new();

        public List<TaskWorkspaceMemberViewModel> WorkspaceMembers { get; set; } = new();
    }
}