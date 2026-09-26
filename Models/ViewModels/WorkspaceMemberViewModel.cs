using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class WorkspaceMemberViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string? Email { get; set; }

        public WorkspaceRole Role { get; set; }

        public DateTime JoinedAt { get; set; }

        public bool CanBeRemovedByCurrentUser { get; set; }
    }
}
