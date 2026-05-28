using Movie_Advisor.Data;
using Movie_Advisor.Models;
using Microsoft.EntityFrameworkCore;

namespace Movie_Advisor.Services
{
    public interface IActivityService
    {
        Task LogActivityAsync(string actionType, string? userName = null, string? description = null, int? entityId = null, string? entityType = null);
        Task<List<ActivityLog>> GetRecentActivitiesAsync(int count = 10);
        Task<List<ActivityLog>> GetActivitiesByTypeAsync(string actionType, int count = 50);
        Task<List<ActivityLog>> GetActivitiesByUserAsync(string userName, int count = 50);
        Task LogActivityAsync(object update, string userName, string v);
    }

    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActivityAsync(string actionType, string? userName = null, string? description = null, int? entityId = null, string? entityType = null)
        {
            try
            {
                var activity = new ActivityLog
                {
                    ActionType = actionType,
                    UserName = userName ?? "System",
                    Description = description,
                    EntityId = entityId,
                    EntityType = entityType,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress()
                };

                _context.ActivityLogs.Add(activity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - activity logging should never break the app
                Console.WriteLine($"Error logging activity: {ex.Message}");
            }
        }

        public async Task<List<ActivityLog>> GetRecentActivitiesAsync(int count = 10)
        {
            return await _context.ActivityLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetActivitiesByTypeAsync(string actionType, int count = 50)
        {
            return await _context.ActivityLogs
                .Where(a => a.ActionType == actionType)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetActivitiesByUserAsync(string userName, int count = 50)
        {
            return await _context.ActivityLogs
                .Where(a => a.UserName == userName)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        private string? GetClientIpAddress()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return null;

                var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                }

                return ipAddress;
            }
            catch
            {
                return null;
            }
        }

        public Task LogActivityAsync(object update, string userName, string v)
        {
            throw new NotImplementedException();
        }
    }

    // Extension methods to make logging easier
    public static class ActivityServiceExtensions
    {
        public static async Task LogUserRegistrationAsync(this IActivityService service, string email)
        {
            await service.LogActivityAsync(
                ActivityType.UserRegistered,
                email,
                $"New user registered: {email}",
                entityType: "User"
            );
        }

        public static async Task LogUserLoginAsync(this IActivityService service, string userName)
        {
            await service.LogActivityAsync(
                ActivityType.UserLogin,
                userName,
                $"User logged in: {userName}",
                entityType: "User"
            );
        }

        public static async Task LogUserDeletedAsync(this IActivityService service, string userName, string deletedBy)
        {
            await service.LogActivityAsync(
                ActivityType.UserDeleted,
                deletedBy,
                $"User deleted: {userName}",
                entityType: "User"
            );
        }

        public static async Task LogMovieAddedAsync(this IActivityService service, string movieTitle, string addedBy, int movieId)
        {
            await service.LogActivityAsync(
                ActivityType.MovieAdded,
                addedBy,
                $"Movie added: \"{movieTitle}\"",
                movieId,
                "Movie"
            );
        }

        public static async Task LogMovieDeletedAsync(this IActivityService service, string movieTitle, string deletedBy)
        {
            await service.LogActivityAsync(
                ActivityType.MovieDeleted,
                deletedBy,
                $"Movie deleted: \"{movieTitle}\"",
                entityType: "Movie"
            );
        }

        public static async Task LogCommentPostedAsync(this IActivityService service, string userName, string movieTitle, int commentId)
        {
            await service.LogActivityAsync(
                ActivityType.CommentPosted,
                userName,
                $"Comment posted on \"{movieTitle}\"",
                commentId,
                "Comment"
            );
        }

        public static async Task LogRatingAddedAsync(this IActivityService service, string userName, string movieTitle, decimal rating)
        {
            await service.LogActivityAsync(
                ActivityType.RatingAdded,
                userName,
                $"Rated \"{movieTitle}\" {rating} stars",
                entityType: "Rating"
            );
        }
    }
}