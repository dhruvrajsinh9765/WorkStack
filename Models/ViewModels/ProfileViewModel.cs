using Microsoft.AspNetCore.Mvc;

namespace WorkStack.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public int WorkspaceCount { get; set; }
    }
}