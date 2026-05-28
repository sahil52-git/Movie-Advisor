using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movie_Advisor.Models
{
    [Table("ACTIVITY_LOGS")]
    public class ActivityLog
    {
        [Key]
        [Column("LOG_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("USER_NAME")]
        [StringLength(100)]
        public string? UserName { get; set; }

        [Required]
        [Column("ACTION_TYPE")]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [Column("DESCRIPTION")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("TIMESTAMP")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Column("ENTITY_ID")]
        public int? EntityId { get; set; }

        [Column("ENTITY_TYPE")]
        [StringLength(50)]
        public string? EntityType { get; set; }

        [Column("IP_ADDRESS")]
        [StringLength(50)]
        public string? IpAddress { get; set; }
    }

    // Action type constants for consistency
    public static class ActivityType
    {
        // User Actions
        public const string UserRegistered = "USER_REGISTERED";
        public const string UserLogin = "USER_LOGIN";
        public const string UserLogout = "USER_LOGOUT";
        public const string UserDeleted = "USER_DELETED";
        public const string UserUpdated = "USER_UPDATED";
        public const string UserBanned = "USER_BANNED";
        public const string UserUnbanned = "USER_UNBANNED";

        // Movie Actions
        public const string MovieAdded = "MOVIE_ADDED";
        public const string MovieUpdated = "MOVIE_UPDATED";
        public const string MovieDeleted = "MOVIE_DELETED";

        // Comment Actions
        public const string CommentPosted = "COMMENT_POSTED";
        public const string CommentApproved = "COMMENT_APPROVED";
        public const string CommentDeleted = "COMMENT_DELETED";
        public const string CommentReported = "COMMENT_REPORTED";

        // Rating Actions
        public const string RatingAdded = "RATING_ADDED";
        public const string RatingUpdated = "RATING_UPDATED";
        public const string RatingDeleted = "RATING_DELETED";

        // Admin Actions
        public const string AdminAccess = "ADMIN_ACCESS";
        public const string SettingsChanged = "SETTINGS_CHANGED";
    }
}