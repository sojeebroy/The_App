using System.ComponentModel.DataAnnotations;
using static The_App.Models.User;

namespace The_App.Models
{
    public class DashboardViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
