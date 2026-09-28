namespace WorkStack.Models.ViewModels
{
    public class WorkspaceBoardsViewModel
    {
        public int WorkspaceId { get; set; }

        public string WorkspaceName { get; set; } = string.Empty;

        public bool CanManageBoards { get; set; }

        public string Search { get; set; } = string.Empty;

        public IReadOnlyList<BoardListItemViewModel> Boards { get; set; }
            = Array.Empty<BoardListItemViewModel>();
    }

    public class BoardListItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}