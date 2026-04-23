using System.ComponentModel.DataAnnotations;

namespace The_App.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Company Name")]
        public string CompanyName{ get; set; } = string.Empty;

        [Display(Name = "Designation")]
        public string CompanyDesignation { get; set; } = string.Empty;

        [Required]
        [MinLength(4, ErrorMessage = "Password must be at least 4 character.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;


    }
}
