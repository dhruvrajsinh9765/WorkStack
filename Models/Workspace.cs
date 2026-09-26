using Microsoft.AspNetCore.Identity;

namespace WorkStack.Models
{
    public class Workspace
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string OwnerId { get; set; } = string.Empty;

        public IdentityUser Owner { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();

        public ICollection<Board> Boards { get; set; } = new List<Board>();
    }
}
