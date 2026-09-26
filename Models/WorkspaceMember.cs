using Microsoft.AspNetCore.Identity;
using WorkStack.Models.Enums;

namespace WorkStack.Models
{
    public class WorkspaceMember
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public IdentityUser User { get; set; } = null!;

        public WorkspaceRole Role { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
