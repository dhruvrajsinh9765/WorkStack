using System.ComponentModel.DataAnnotations;

namespace WorkStack.Models.ViewModels
{
    public class BoardFormViewModel
    {
        [Required(ErrorMessage = "Board name is required.")]
        [StringLength(
            100,
            ErrorMessage = "Board name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(
            500,
            ErrorMessage = "Board description cannot be longer than 500 characters.")]
        public string? Description { get; set; }
    }
}