using System.ComponentModel.DataAnnotations;

namespace WorkStack.Models.ViewModels
{
    public class CreateWorkspaceViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
