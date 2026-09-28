using System.ComponentModel.DataAnnotations;
using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class AddWorkspaceMemberViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(
            256,
            ErrorMessage = "Email address cannot be longer than 256 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Workspace role is required.")]
        [EnumDataType(
            typeof(WorkspaceRole),
            ErrorMessage = "Select a valid workspace role.")]
        public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
    }
}