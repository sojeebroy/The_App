using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace The_App.Models
{
    [Index(nameof(Email), IsUnique = true, Name = "unique_email_")]
    public class User : IdentityUser
    {
        public enum UserStatus
        {
            Unverified,
            Active,
            Blocked
        }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string CompanyDesignation { get; set; } = string.Empty;

        public UserStatus Status { get; set; } = UserStatus.Unverified;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public string? EmailVerificationToken { get; set; }
    }
}