using Movie_Advisor.Models;
using MovieAdvisor.Models;

namespace MovieAdvisor.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Existing properties - KEEP THESE
        public int TotalMovies { get; set; }
        public int TotalUsers { get; set; }
        public int TotalRatings { get; set; }
        public int TotalComments { get; set; }
        public int PendingComments { get; set; }
        public List<Movie> RecentMovies { get; set; } = new();

        // NEW properties - ADD THESE
        public int NewUsersThisMonth { get; set; }
        public int NewMoviesThisMonth { get; set; }
        public int ActiveUsersToday { get; set; }

        public decimal MoviesGrowthPercentage { get; set; }
        public decimal UsersGrowthPercentage { get; set; }
        public decimal RatingsGrowthPercentage { get; set; }
        public decimal CommentsChangePercentage { get; set; }

        public List<ActivityLog> RecentActivities { get; set; } = new();

        public List<ChartDataPoint> UserGrowthData { get; set; } = new();
        public Dictionary<string, int> ContentDistribution { get; set; } = new();
    }

    // ADD these new helper classes at the bottom of the file
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class ActivityDisplayModel
    {
        public int Id { get; set; }
        public string Icon { get; set; } = "fa-info";
        public string IconColor { get; set; } = "#00BCD4";
        public string IconBackground { get; set; } = "rgba(0, 188, 212, 0.2)";
        public string Text { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;

        public static ActivityDisplayModel FromActivityLog(ActivityLog log)
        {
            var model = new ActivityDisplayModel
            {
                Id = log.Id,
                Text = log.Description ?? "Activity logged",
                TimeAgo = GetTimeAgo(log.Timestamp),
                ActionType = log.ActionType
            };

            switch (log.ActionType)
            {
                case ActivityType.UserRegistered:
                    model.Icon = "fa-user-plus";
                    model.IconColor = "#30D158";
                    model.IconBackground = "rgba(48, 209, 88, 0.2)";
                    break;

                case ActivityType.UserDeleted:
                case ActivityType.UserBanned:
                    model.Icon = "fa-user-times";
                    model.IconColor = "#dc3545";
                    model.IconBackground = "rgba(220, 53, 69, 0.2)";
                    break;

                case ActivityType.UserLogin:
                    model.Icon = "fa-sign-in-alt";
                    model.IconColor = "#8b5cf6";
                    model.IconBackground = "rgba(139, 92, 246, 0.2)";
                    break;

                case ActivityType.MovieAdded:
                    model.Icon = "fa-film";
                    model.IconColor = "#00BCD4";
                    model.IconBackground = "rgba(0, 188, 212, 0.2)";
                    break;

                case ActivityType.MovieDeleted:
                    model.Icon = "fa-trash";
                    model.IconColor = "#dc3545";
                    model.IconBackground = "rgba(220, 53, 69, 0.2)";
                    break;

                case ActivityType.CommentPosted:
                    model.Icon = "fa-comment";
                    model.IconColor = "#8b5cf6";
                    model.IconBackground = "rgba(139, 92, 246, 0.2)";
                    break;

                case ActivityType.CommentReported:
                    model.Icon = "fa-flag";
                    model.IconColor = "#dc3545";
                    model.IconBackground = "rgba(220, 53, 69, 0.2)";
                    break;

                case ActivityType.RatingAdded:
                    model.Icon = "fa-star";
                    model.IconColor = "#FF9500";
                    model.IconBackground = "rgba(255, 149, 0, 0.2)";
                    break;

                case ActivityType.AdminAccess:
                    model.Icon = "fa-shield-alt";
                    model.IconColor = "#00BCD4";
                    model.IconBackground = "rgba(0, 188, 212, 0.2)";
                    break;

                default:
                    model.Icon = "fa-info-circle";
                    model.IconColor = "#00BCD4";
                    model.IconBackground = "rgba(0, 188, 212, 0.2)";
                    break;
            }

            return model;
        }

        private static string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) != 1 ? "s" : "")} ago";

            return timestamp.ToString("MMM dd, yyyy");
        }
    }
}