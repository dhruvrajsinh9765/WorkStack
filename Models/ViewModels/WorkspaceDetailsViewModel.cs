using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class WorkspaceDetailsViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public WorkspaceRole Role { get; set; }
    }
}
