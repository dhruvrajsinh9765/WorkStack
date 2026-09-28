namespace WorkStack.Models.ViewModels
{
    public class ListViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Position { get; set; }

        public int TaskCount { get; set; }

        public List<TaskCardViewModel> Tasks { get; set; } = new();
    }
}