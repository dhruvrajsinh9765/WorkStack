namespace WorkStack.Models.ViewModels
{
    public class CommentViewModel
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}