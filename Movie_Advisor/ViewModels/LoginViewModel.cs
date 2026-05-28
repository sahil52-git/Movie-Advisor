using MovieAdvisor.Models;
using System.ComponentModel.DataAnnotations;

namespace Movie_Advisor.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        public required string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public required string Password { get; set; } = string.Empty;
        public required bool RememberMe { get; set; }
    }
}