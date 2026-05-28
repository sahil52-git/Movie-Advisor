using System.ComponentModel.DataAnnotations;

namespace Movie_Advisor.ViewModels
{
    public class UserProfileViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        [StringLength(255)]
        public string? Name { get; set; }

        [Display(Name = "Member Since")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginDate { get; set; }

        public bool IsGoogleUser { get; set; }
    }
}