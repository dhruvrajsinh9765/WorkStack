namespace WorkStack.Models.ViewModels
{
    public class BoardDetailsViewModel
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }

        public string WorkspaceName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool CanManageBoards { get; set; }

        public List<ListViewModel> Lists { get; set; } = new();

        public bool CanManageLists { get; set; }
    }
}