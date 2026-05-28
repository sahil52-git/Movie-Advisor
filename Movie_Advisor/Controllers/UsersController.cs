using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movie_Advisor.Data;
using Movie_Advisor.Models;

namespace Movie_Advisor.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 20)
        {
            try
            {
                // Use AsNoTracking for better performance on read-only queries
                var usersQuery = _context.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    usersQuery = usersQuery.Where(u =>
                        (u.Username != null && u.Username.Contains(search)) ||
                        (u.Email != null && u.Email.Contains(search)) ||
                        (u.FirstName != null && u.FirstName.Contains(search)) ||
                        (u.LastName != null && u.LastName.Contains(search))
                    );
                }

                // Get statistics separately to avoid loading all users
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var pendingUsers = await _context.Users.CountAsync(u => !u.IsVerified);
                var suspendedUsers = await _context.Users.CountAsync(u => !u.IsActive);

                ViewBag.TotalUsers = totalUsers;
                ViewBag.ActiveUsers = activeUsers;
                ViewBag.PendingUsers = pendingUsers;
                ViewBag.SuspendedUsers = suspendedUsers;

                var totalFiltered = await usersQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalFiltered / (double)pageSize);

                // Select only the fields you need and project to anonymous type first
                var users = await usersQuery
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new User
                    {
                        Id = u.Id,
                        Username = u.Username ?? "",
                        Email = u.Email ?? "",
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        IsVerified = u.IsVerified,
                        DateOfBirth = u.DateOfBirth,
                        Gender = u.Gender,
                        Bio = u.Bio,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        LastLogin = u.LastLogin
                    })
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalFiltered = totalFiltered;
                ViewBag.SearchQuery = search;

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users. Exception details: {Message}", ex.Message);
                ViewBag.ErrorMessage = "Error loading users. Please try again.";
                return View(new List<User>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int userId, string role)
        {
            try
            {
                _logger.LogInformation("UpdateRole called with userId: {UserId}, role: {Role}", userId, role);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (Enum.TryParse<Movie_Advisor.Models.Role>(role, true, out var newRole))
                {
                    user.Role = newRole;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated role for user {UserId} to {Role}", userId, role);
                    return Json(new { success = true, message = "Role updated successfully" });
                }

                return Json(new { success = false, message = "Invalid role" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role");
                return Json(new { success = false, message = "Error updating role" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int userId)
        {
            try
            {
                _logger.LogInformation("ToggleStatus called with userId: {UserId}", userId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Toggled status for user {UserId} to {Status}", userId, user.IsActive);
                return Json(new
                {
                    success = true,
                    message = user.IsActive ? "User activated" : "User suspended",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status");
                return Json(new { success = false, message = "Error updating status" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                _logger.LogInformation("DeleteUser called with userId: {UserId}", userId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == currentUserId)
                {
                    return Json(new { success = false, message = "You cannot delete your own account" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted user {UserId} - {Username}", userId, user.Username);
                return Json(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return Json(new { success = false, message = "Error deleting user" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int userId)
        {
            try
            {
                _logger.LogInformation("GetUserDetails called with userId: {UserId}", userId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                return Json(new
                {
                    success = true,
                    user = new
                    {
                        user.Id,
                        Username = user.Username ?? "",
                        Email = user.Email ?? "",
                        user.FirstName,
                        user.LastName,
                        Role = user.Role.ToString(),
                        user.IsActive,
                        user.IsVerified,
                        user.DateOfBirth,
                        user.Gender,
                        user.Bio,
                        user.CreatedAt,
                        user.LastLogin
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user details for userId: {UserId}", userId);
                return Json(new { success = false, message = "Error loading user details" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(int userId, string username, string email, string role, string isActive)
        {
            try
            {
                _logger.LogInformation("UpdateUser called with userId: {UserId}, username: {Username}, isActive: {IsActive}",
                    userId, username, isActive);

                var existingUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Id != userId);

                if (existingUsername != null)
                {
                    return Json(new { success = false, message = "Username already in use" });
                }

                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.Id != userId);

                if (existingEmail != null)
                {
                    return Json(new { success = false, message = "Email already in use" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.Username = username;
                user.Email = email;

                if (Enum.TryParse<Movie_Advisor.Models.Role>(role, true, out var newRole))
                {
                    user.Role = newRole;
                }

                user.IsActive = string.Equals(isActive, "true", StringComparison.OrdinalIgnoreCase);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user {UserId} - {Username}, IsActive: {IsActive}",
                    userId, username, user.IsActive);

                return Json(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user. UserId: {UserId}, IsActive parameter: {IsActive}",
                    userId, isActive);
                return Json(new { success = false, message = $"Error updating user: {ex.Message}" });
            }
        }
    }
}