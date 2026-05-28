using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movie_Advisor.Data;
using Movie_Advisor.Models;
using Movie_Advisor.Services;
using MovieAdvisor.ViewModels;

namespace Movie_Advisor.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityService _activityService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, IActivityService activityService, ILogger<AdminController> logger)
        {
            _context = context;
            _activityService = activityService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var model = new AdminDashboardViewModel();
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfLastMonth = startOfMonth.AddMonths(-1);
                var startOfToday = now.Date;

                model.TotalMovies = await _context.Movies.CountAsync();
                model.TotalUsers = await _context.Users.CountAsync();
                model.TotalRatings = 0;
                model.NewUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= startOfMonth);
                model.NewMoviesThisMonth = await _context.Movies.CountAsync(m => m.CreatedAt >= startOfMonth);
                model.ActiveUsersToday = await _context.Users.CountAsync(u => u.LastLogin.HasValue && u.LastLogin.Value >= startOfToday);

                var usersLastMonth = await _context.Users.CountAsync(u => u.CreatedAt >= startOfLastMonth && u.CreatedAt < startOfMonth);
                model.UsersGrowthPercentage = CalculateGrowthPercentage(usersLastMonth, model.NewUsersThisMonth);

                var moviesLastMonth = await _context.Movies.CountAsync(m => m.CreatedAt >= startOfLastMonth && m.CreatedAt < startOfMonth);
                model.MoviesGrowthPercentage = CalculateGrowthPercentage(moviesLastMonth, model.NewMoviesThisMonth);

                model.RatingsGrowthPercentage = 0;
                model.CommentsChangePercentage = 0;
                model.RecentActivities = await GetCombinedActivitiesAsync(15);

                for (int i = 6; i >= 0; i--)
                {
                    var date = now.Date.AddDays(-i);
                    var nextDate = date.AddDays(1);
                    var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= date && u.CreatedAt < nextDate);
                    var activeUsers = await _context.Users.CountAsync(u => u.LastLogin.HasValue && u.LastLogin.Value >= date && u.LastLogin.Value < nextDate);

                    model.UserGrowthData.Add(new ChartDataPoint
                    {
                        Label = date.ToString("ddd"),
                        NewUsers = newUsers,
                        ActiveUsers = activeUsers
                    });
                }

                model.ContentDistribution = new Dictionary<string, int> { { "Movies", model.TotalMovies }, { "Users", model.TotalUsers } };
                return View(model);
            }
            catch (Exception ex)
            {
                return Content($@"<html><head><title>Database Error</title></head><body><h1>⚠️ Database Error</h1><p>{ex.Message}</p></body></html>", "text/html");
            }
        }

        private async Task<List<ActivityLog>> GetCombinedActivitiesAsync(int count)
        {
            try
            {
                var activities = new List<ActivityLog>();
                var systemActivities = await _context.ActivityLogs
                    .Where(a => a.ActionType != "AdminAccess" && a.ActionType != "Admin_Access")
                    .OrderByDescending(a => a.Timestamp)
                    .Take(count * 2)
                    .ToListAsync();
                activities.AddRange(systemActivities);

                var recentMovies = await _context.Movies
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                foreach (var movie in recentMovies)
                {
                    activities.Add(new ActivityLog
                    {
                        Id = movie.Id + 1000000,
                        ActionType = "MovieAdded",
                        UserName = "Admin",
                        Description = $"New movie added: <strong>{movie.Title}</strong> ({movie.ReleaseDate?.Year ?? 0})",
                        Timestamp = movie.CreatedAt
                    });
                }
                return activities.OrderByDescending(a => a.Timestamp).Take(count).ToList();
            }
            catch (Exception)
            {
                return await _context.ActivityLogs
                    .Where(a => a.ActionType != "AdminAccess" && a.ActionType != "Admin_Access")
                    .OrderByDescending(a => a.Timestamp)
                    .Take(count)
                    .ToListAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userName = User.Identity?.Name ?? "Admin";
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userName);
                if (user == null) return RedirectToAction("Dashboard");

                var model = new AdminProfileViewModel
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.Name ?? user.Username,
                    Bio = user.Bio,
                    ProfileImageUrl = user.ProfilePicture,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    TotalLogins = await _context.ActivityLogs.CountAsync(a => a.UserName == userName && a.ActionType == "Login"),
                    TotalActivities = await _context.ActivityLogs.CountAsync(a => a.UserName == userName)
                };
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load profile: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData()
        {
            try
            {
                var now = DateTime.UtcNow;
                var userGrowthData = new List<ChartDataPoint>();

                for (int i = 6; i >= 0; i--)
                {
                    var date = now.Date.AddDays(-i);
                    var nextDate = date.AddDays(1);
                    var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= date && u.CreatedAt < nextDate);
                    var activeUsers = await _context.Users.CountAsync(u => u.LastLogin.HasValue && u.LastLogin.Value >= date && u.LastLogin.Value < nextDate);
                    userGrowthData.Add(new ChartDataPoint
                    {
                        Label = date.ToString("ddd"),
                        NewUsers = newUsers,
                        ActiveUsers = activeUsers
                    });
                }

                var contentDistribution = new Dictionary<string, int>
                {
                    { "Movies", await _context.Movies.CountAsync() },
                    { "Users", await _context.Users.CountAsync() }
                };

                return Json(new { userGrowthData, contentDistribution });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredChartData(string period)
        {
            try
            {
                var now = DateTime.UtcNow;
                var userGrowthData = new List<ChartDataPoint>();

                switch (period)
                {
                    case "Last 7 Days":
                        for (int i = 6; i >= 0; i--)
                        {
                            var date = now.Date.AddDays(-i);
                            var nextDate = date.AddDays(1);
                            userGrowthData.Add(new ChartDataPoint
                            {
                                Label = date.ToString("ddd"),
                                NewUsers = await _context.Users.CountAsync(u => u.CreatedAt >= date && u.CreatedAt < nextDate),
                                ActiveUsers = await _context.Users.CountAsync(u => u.LastLogin.HasValue && u.LastLogin.Value >= date && u.LastLogin.Value < nextDate)
                            });
                        }
                        break;

                    case "Last 30 Days":
                        for (int i = 3; i >= 0; i--)
                        {
                            var startDate = now.Date.AddDays(-((i + 1) * 7));
                            var endDate = now.Date.AddDays(-(i * 7));
                            userGrowthData.Add(new ChartDataPoint
                            {
                                Label = $"Week {4 - i}",
                                NewUsers = await _context.Users.CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt < endDate),
                                ActiveUsers = await _context.Users.CountAsync(u => u.LastLogin.HasValue && u.LastLogin.Value >= startDate && u.LastLogin.Value < endDate)
                            });
                        }
                        break;

                    case "Last 3 Months":
                        for (int i = 2; i >= 0; i--)
                        {
                            var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                            var endDate = startDate.AddMonths(1);
                            userGrowthData.Add(new ChartDataPoint
                            {
                                Label = startDate.ToString("MMM"),
                                NewUsers = await _context.Users.CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt < endDate),
                                ActiveUsers = await _context.Users.CountAsync(u => u.LastLogin.HasValue && u.LastLogin.Value >= startDate && u.LastLogin.Value < endDate)
                            });
                        }
                        break;

                    default:
                        return await GetChartData();
                }

                return Json(new { userGrowthData });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(AdminProfileViewModel model)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Admin";
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userName);
                if (user == null) return Json(new { success = false, message = "User not found" });

                user.Name = model.FullName;
                user.Email = model.Email;
                user.Bio = model.Bio;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await _activityService.LogActivityAsync(ActivityType.UserUpdated, userName, $"{userName} updated their profile");

                return Json(new { success = true, message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AccountSettings()
        {
            try
            {
                var userName = User.Identity?.Name ?? "Admin";
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userName);
                if (user == null) return RedirectToAction("Dashboard");

                var model = new AccountSettingsViewModel
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    EmailNotifications = true,
                    SecurityAlerts = true,
                    ActivityDigest = false,
                    TwoFactorEnabled = false
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load settings: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Admin";
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userName);
                if (user == null) return Json(new { success = false, message = "User not found" });
                if (user.PasswordHash != model.CurrentPassword) return Json(new { success = false, message = "Current password is incorrect" });

                user.PasswordHash = model.NewPassword;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await _activityService.LogActivityAsync(ActivityType.UserUpdated, userName, $"{userName} changed their password");

                return Json(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNotificationSettings(NotificationSettingsViewModel model)
        {
            try
            {
                await _activityService.LogActivityAsync(ActivityType.SettingsChanged, User.Identity?.Name ?? "Admin", $"{User.Identity?.Name ?? "Admin"} updated notification settings");
                return Json(new { success = true, message = "Settings updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            try
            {
                var model = new NotificationsViewModel();
                var recentActivities = await GetCombinedActivitiesAsync(50);

                model.Notifications = recentActivities.Select(a => new NotificationItem
                {
                    Id = a.Id,
                    Type = a.ActionType,
                    Title = GetNotificationTitle(a.ActionType),
                    Message = a.Description,
                    Timestamp = a.Timestamp,
                    IsRead = false,
                    Icon = GetIconForActivityType(a.ActionType),
                    Color = GetColorForActivityType(a.ActionType)
                }).ToList();

                model.UnreadCount = model.Notifications.Count(n => !n.IsRead);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load notifications: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                await _activityService.LogActivityAsync(ActivityType.SettingsChanged, User.Identity?.Name ?? "Admin", $"{User.Identity?.Name ?? "Admin"} marked all notifications as read");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivities(int count = 10)
        {
            try
            {
                var activities = await GetCombinedActivitiesAsync(count);
                return Json(activities.Select(a => ActivityDisplayModel.FromActivityLog(a)).ToList());
            }
            catch (Exception)
            {
                return Json(new List<ActivityDisplayModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                return Json(new
                {
                    totalMovies = await _context.Movies.CountAsync(),
                    totalUsers = await _context.Users.CountAsync(),
                    totalRatings = 0,
                    pendingComments = await _context.Comments.CountAsync(c => !c.IsApproved),
                    newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= startOfMonth),
                    activeUsersToday = await _context.Users.CountAsync(u => u.LastLogin.HasValue && u.LastLogin.Value >= now.Date)
                });
            }
            catch (Exception)
            {
                return Json(new { error = "Failed to fetch stats" });
            }
        }

        public IActionResult Settings() => View();

        public ApplicationDbContext Get_context() => _context;

        public IActionResult NewMovie() => View();

        public async Task<IActionResult> ActivityLog(string search)
        {
            try
            {
                var dbLogs = await _context.ActivityLogs
                    .Where(a => a.ActionType != "AdminAccess" && a.ActionType != "Admin_Access")
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();

                var movieActivities = await _context.Movies
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                var combinedActivities = new List<ActivityLog>();
                combinedActivities.AddRange(dbLogs);

                foreach (var movie in movieActivities)
                {
                    combinedActivities.Add(new ActivityLog
                    {
                        Id = movie.Id + 1000000,
                        ActionType = "MovieAdded",
                        UserName = "Admin",
                        Description = $"New movie added: <strong>{movie.Title}</strong> ({movie.ReleaseDate?.Year ?? 0})",
                        Timestamp = movie.CreatedAt
                    });
                }

                if (!string.IsNullOrEmpty(search))
                {
                    combinedActivities = combinedActivities
                        .Where(x => x.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                   x.ActionType.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                   x.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                return View(combinedActivities.OrderByDescending(x => x.Timestamp).Take(100).ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ActivityLog: {ex.Message}");
                return View(new List<ActivityLog>());
            }
        }

        [HttpGet, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Comments()
        {
            try
            {
                var model = new AdminCommentsViewModel();
                var now = DateTime.UtcNow;

                var allComments = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Movie)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var commentViewModels = allComments.Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    MovieId = c.MovieId,
                    MovieTitle = c.Movie?.Title ?? "Unknown Movie",
                    UserId = c.UserId,
                    UserName = c.User?.Username ?? "Unknown User",
                    CommentText = c.CommentText,
                    IsApproved = true, // All comments are now approved by default
                    CreatedAt = c.CreatedAt,
                    TimeAgo = GetTimeAgoString(c.CreatedAt)
                }).ToList();

                model.RecentComments = commentViewModels;
                model.TotalComments = commentViewModels.Count;
                model.PendingCount = 0;
                model.ApprovedCount = commentViewModels.Count;
                model.TodayCount = commentViewModels.Count(c => c.CreatedAt.Date == now.Date);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading comments management page");
                TempData["Error"] = "Error loading comments. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost, Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var comment = await _context.Comments
                    .Include(c => c.Movie)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null) return Json(new { success = false, message = "Comment not found" });

                var movieTitle = comment.Movie?.Title ?? "Unknown Movie";
                var userName = comment.User?.Username ?? "Unknown User";

                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
                await _activityService.LogActivityAsync(ActivityType.UserUpdated, User.Identity?.Name ?? "Admin", $"Deleted comment on <strong>{movieTitle}</strong> by {userName}");

                return Json(new { success = true, message = "Comment deleted successfully", commentId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {Id}", id);
                return Json(new { success = false, message = "Error deleting comment" });
            }
        }

        [HttpGet, Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCommentStats()
        {
            try
            {
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                return Json(new
                {
                    total = await _context.Comments.CountAsync(),
                    today = await _context.Comments.CountAsync(c => c.CreatedAt.Date == now.Date),
                    thisMonth = await _context.Comments.CountAsync(c => c.CreatedAt >= startOfMonth)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment stats");
                return Json(new { error = ex.Message });
            }
        }

        private decimal CalculateGrowthPercentage(int previousValue, int currentValue)
        {
            if (previousValue == 0)
                return currentValue > 0 ? 100 : 0;

            return Math.Round(((decimal)(currentValue - previousValue) / previousValue) * 100, 1);
        }

        private string GetNotificationTitle(string actionType)
        {
            return actionType?.ToLower() switch
            {
                "login" or "user_login" => "User Login",
                "logout" or "user_logout" => "User Logout",
                "create" or "movie_added" or "movieadded" => "New Movie Added",
                "update" or "user_updated" or "movie_updated" => "Content Updated",
                "delete" or "movie_deleted" => "Content Deleted",
                _ => "System Activity"
            };
        }

        private static string GetIconForActivityType(string actionType)
        {
            return actionType?.ToLower() switch
            {
                "login" or "user_login" => "fa-sign-in-alt",
                "logout" or "user_logout" => "fa-sign-out-alt",
                "create" or "movie_added" or "movieadded" => "fa-film",
                "update" or "user_updated" or "movie_updated" => "fa-edit",
                "delete" or "movie_deleted" => "fa-trash",
                "adminaccess" or "admin_access" => "fa-user-shield",
                "rating" or "rating_added" => "fa-star",
                "comment" or "comment_posted" => "fa-comment",
                "report" or "comment_reported" => "fa-flag",
                _ => "fa-info-circle"
            };
        }

        private static string GetColorForActivityType(string actionType)
        {
            return actionType?.ToLower() switch
            {
                "login" or "user_login" => "success",
                "logout" or "user_logout" => "secondary",
                "create" or "movie_added" or "movieadded" => "primary",
                "update" or "user_updated" or "movie_updated" => "info",
                "delete" or "movie_deleted" => "danger",
                "adminaccess" or "admin_access" => "warning",
                "rating" or "rating_added" => "warning",
                "comment" or "comment_posted" => "info",
                "report" or "comment_reported" => "danger",
                _ => "secondary"
            };
        }

        private string GetTimeAgoString(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;

            if (timeSpan.TotalMinutes < 1) return "just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)}w ago";

            return timestamp.ToString("MMM dd, yyyy");
        }
    }

    public class ActivityDisplayModel
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string IconColor { get; set; } = string.Empty;
        public string IconBackground { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        public static ActivityDisplayModel FromActivityLog(ActivityLog log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            var model = new ActivityDisplayModel
            {
                Id = log.Id,
                ActionType = log.ActionType,
                UserName = log.UserName,
                Description = log.Description,
                Timestamp = log.Timestamp,
                TimeAgo = GetTimeAgo(log.Timestamp),
                Text = log.Description,
                Icon = GetIconForActivityType(log.ActionType)
            };

            var colors = GetColorsForActivityType(log.ActionType);
            model.IconColor = colors.color;
            model.IconBackground = colors.background;

            return model;
        }

        private static string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;

            if (timeSpan.TotalMinutes < 1) return "just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)}w ago";

            return timestamp.ToString("MMM dd, yyyy");
        }

        private static string GetIconForActivityType(string actionType)
        {
            return actionType?.ToLower() switch
            {
                "login" or "user_login" => "fa-sign-in-alt",
                "logout" or "user_logout" => "fa-sign-out-alt",
                "create" or "movie_added" or "movieadded" => "fa-film",
                "update" or "user_updated" or "movie_updated" => "fa-edit",
                "delete" or "movie_deleted" => "fa-trash",
                "adminaccess" or "admin_access" => "fa-user-shield",
                "rating" or "rating_added" => "fa-star",
                "comment" or "comment_posted" => "fa-comment",
                "report" or "comment_reported" => "fa-flag",
                _ => "fa-info-circle"
            };
        }

        private static (string color, string background) GetColorsForActivityType(string actionType)
        {
            return actionType?.ToLower() switch
            {
                "login" or "user_login" => ("#30D158", "rgba(48, 209, 88, 0.2)"),
                "logout" or "user_logout" => ("#b0b0b0", "rgba(176, 176, 176, 0.2)"),
                "create" or "movie_added" or "movieadded" => ("#00BCD4", "rgba(0, 188, 212, 0.2)"),
                "update" or "user_updated" or "movie_updated" => ("#8b5cf6", "rgba(139, 92, 246, 0.2)"),
                "delete" or "movie_deleted" => ("#dc3545", "rgba(220, 53, 69, 0.2)"),
                "adminaccess" or "admin_access" => ("#FF9500", "rgba(255, 149, 0, 0.2)"),
                "rating" or "rating_added" => ("#FF9500", "rgba(255, 149, 0, 0.2)"),
                "comment" or "comment_posted" => ("#8b5cf6", "rgba(139, 92, 246, 0.2)"),
                "report" or "comment_reported" => ("#dc3545", "rgba(220, 53, 69, 0.2)"),
                _ => ("#b0b0b0", "rgba(176, 176, 176, 0.2)")
            };
        }
    }
}