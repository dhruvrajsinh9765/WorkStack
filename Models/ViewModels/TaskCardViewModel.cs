using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class TaskCardViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public TaskPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public List<string> AssigneeNames { get; set; } = new();
    }
}