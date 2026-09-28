using System.ComponentModel.DataAnnotations;
using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class EditTaskViewModel
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }

        public int BoardId { get; set; }

        public int ListId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(5000)]
        public string? Description { get; set; }

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }
    }
}