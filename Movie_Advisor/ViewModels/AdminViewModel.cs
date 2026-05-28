using System.ComponentModel.DataAnnotations;

namespace MovieAdvisor.ViewModels
{
    public class AdminCommentsViewModel
    {
        public List<CommentViewModel> PendingComments { get; set; } = new();
        public List<CommentViewModel> ApprovedComments { get; set; } = new();
        public List<CommentViewModel> RecentComments { get; set; } = new();
        public int TotalComments { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int TodayCount { get; set; }
    }

    public class CommentViewModel
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public string UserInitial => UserName?.FirstOrDefault().ToString().ToUpper() ?? "?";
    }

    public class AdminProfileViewModel
    {
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Bio")]
        [MaxLength(500)]
        public string? Bio { get; set; }

        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public int TotalLogins { get; set; }
        public int TotalActivities { get; set; }
    }

    public class AccountSettingsViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Notification Settings
        public bool EmailNotifications { get; set; }
        public bool SecurityAlerts { get; set; }
        public bool ActivityDigest { get; set; }

        // Security Settings
        public bool TwoFactorEnabled { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class NotificationSettingsViewModel
    {
        public bool EmailNotifications { get; set; }
        public bool SecurityAlerts { get; set; }
        public bool ActivityDigest { get; set; }
        public bool CommentNotifications { get; set; }
        public bool RatingNotifications { get; set; }
        public bool ReportNotifications { get; set; }
    }

    public class NotificationsViewModel
    {
        public List<NotificationItem> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
        public int TotalCount => Notifications.Count;
    }

    public class NotificationItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - Timestamp;

                if (timeSpan.TotalMinutes < 1)
                    return "just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                if (timeSpan.TotalDays < 30)
                    return $"{(int)(timeSpan.TotalDays / 7)}w ago";

                return Timestamp.ToString("MMM dd, yyyy");
            }
        }
    }
}