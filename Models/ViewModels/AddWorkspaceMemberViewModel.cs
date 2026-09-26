using System.ComponentModel.DataAnnotations;
using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class AddWorkspaceMemberViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(WorkspaceRole))]
        public WorkspaceRole Role { get; set; }
    }
}
