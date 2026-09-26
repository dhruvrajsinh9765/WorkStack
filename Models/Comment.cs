using Microsoft.AspNetCore.Identity;

namespace WorkStack.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int TaskId { get; set; }

        public Task Task { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public IdentityUser User { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
