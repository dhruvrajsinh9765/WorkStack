using Microsoft.AspNetCore.Identity;
using WorkStack.Models.Enums;

namespace WorkStack.Models
{
    public class Task
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int ListId { get; set; }

        public List List { get; set; } = null!;

        public string CreatorId { get; set; } = string.Empty;

        public IdentityUser Creator { get; set; } = null!;

        public TaskPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
