using System.ComponentModel.DataAnnotations;
using MovieAdvisor.Models;

namespace Movie_Advisor.ViewModels
{
    public class SignupViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100)]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public Role Role { get; set; } 
        public string? RecaptchaToken { get; set; }
    }
}
