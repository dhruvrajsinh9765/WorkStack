namespace WorkStack.Models
{
    public class Board
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<List> Lists { get; set; } = new List<List>();
    }
}
