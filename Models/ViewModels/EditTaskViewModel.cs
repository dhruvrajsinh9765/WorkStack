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

        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(
            200,
            ErrorMessage = "Task title cannot be longer than 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(
            5000,
            ErrorMessage = "Task description cannot be longer than 5000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Task priority is required.")]
        [EnumDataType(
            typeof(TaskPriority),
            ErrorMessage = "Select a valid task priority.")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }
    }
}