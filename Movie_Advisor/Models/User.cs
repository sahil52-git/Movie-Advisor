using MovieAdvisor.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movie_Advisor.Models  // ✅ Keep this
{
    [Table("USERS")]
    public class User
    {
        [Key]
        [Column("USER_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("USERNAME")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("EMAIL")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("PASSWORD_HASH")]
        [StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("FIRST_NAME")]
        [StringLength(50)]
        public string? FirstName { get; set; }

        [Column("LAST_NAME")]
        [StringLength(50)]
        public string? LastName { get; set; }

        [NotMapped]
        public string? Name
        {
            get => !string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName)
                ? $"{FirstName} {LastName}".Trim()
                : null;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var parts = value.Split(' ', 2, StringSplitOptions.TrimEntries);
                    FirstName = parts[0];
                    LastName = parts.Length > 1 ? parts[1] : null;
                }
            }
        }

        [Column("DATE_OF_BIRTH")]
        public DateTime? DateOfBirth { get; set; }

        [Column("GENDER")]
        [StringLength(10)]
        public string? Gender { get; set; }

        [Column("PROFILE_PICTURE")]
        [StringLength(500)]
        public string? ProfilePicture { get; set; }

        [Column("BIO")]
        [StringLength(500)]
        public string? Bio { get; set; }

        [Column("GOOGLE_ID")]
        [StringLength(255)]
        public string? GoogleId { get; set; }

        [Required]
        [Column("ROLE")]
        public Role Role { get; set; } = Role.User;

        [Column("IS_ACTIVE")]
        public bool IsActive { get; set; } = true;

        [Column("IS_VERIFIED")]
        public bool IsVerified { get; set; } = false;

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }

        [Column("LAST_LOGIN")]
        public DateTime? LastLogin { get; set; }

        [Column("PASSWORD_RESET_TOKEN")]
        [StringLength(255)]
        public string? PasswordResetToken { get; set; }

        [Column("PASSWORD_RESET_TOKEN_EXPIRY")]
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // Navigation properties
        //public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<UserPreference>? UserGenrePreferences { get; set; }

        [NotMapped]
        public DateTime CreatedDate
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }

        [NotMapped]
        public string Password
        {
            get => PasswordHash;
            set => PasswordHash = value;
        }

        [NotMapped]
        public DateTime? UpdatedDate
        {
            get => UpdatedAt;
            set => UpdatedAt = value;
        }

        [NotMapped]
        public DateTime? LastLoginDate
        {
            get => LastLogin;
            set => LastLogin = value;
        }
    }

    public enum Role
    {
        User,
        Admin
    }
}