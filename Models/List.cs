namespace WorkStack.Models
{
    public class List
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Position { get; set; }

        public int BoardId { get; set; }

        public Board Board { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}
