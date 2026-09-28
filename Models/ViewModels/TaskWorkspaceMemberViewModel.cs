namespace WorkStack.Models.ViewModels
{
    public class TaskWorkspaceMemberViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public bool IsAssigned { get; set; }
    }
}