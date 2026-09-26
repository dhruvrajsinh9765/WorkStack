using Microsoft.AspNetCore.Identity;

namespace WorkStack.Models
{
    public class TaskAssignee
    {
        public int TaskId { get; set; }

        public Task Task { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public IdentityUser User { get; set; } = null!;

    }
}
