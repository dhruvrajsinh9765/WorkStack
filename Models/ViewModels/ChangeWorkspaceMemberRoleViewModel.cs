using System.ComponentModel.DataAnnotations;
using WorkStack.Models.Enums;

namespace WorkStack.Models.ViewModels
{
    public class ChangeWorkspaceMemberRoleViewModel
    {
        public string? Email { get; set; }

        public WorkspaceRole CurrentRole { get; set; }

        [Required]
        [EnumDataType(typeof(WorkspaceRole))]
        public WorkspaceRole NewRole { get; set; }
    }
}
