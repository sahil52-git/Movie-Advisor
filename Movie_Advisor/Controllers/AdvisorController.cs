using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movie_Advisor.Data;
using Movie_Advisor.Models;
using Movie_Advisor.ViewModels;
using MovieAdvisor.Models;
using MovieAdvisor.ViewModels;
using System.Security.Claims;
using System.Text.Json;
using Movie_Advisor.Services;

namespace MovieAdvisor.Controllers
{
    [Authorize]
    public class AdvisorController : Controller
    {
        private readonly ILogger<AdvisorController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly YouTubeApiService _youtubeService;

        public AdvisorController(
            ILogger<AdvisorController> logger,
            ApplicationDbContext context,
            YouTubeApiService youtubeService)
        {
            _logger = logger;
            _context = context;
            _youtubeService = youtubeService;
        }

        // Add these methods to your existing AdvisorController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment([FromForm] int movieId, [FromForm] string commentText)
        {
            try
            {
                _logger.LogInformation("AddComment called - MovieId: {MovieId}, CommentLength: {Length}",
                    movieId, commentText?.Length ?? 0);

                if (string.IsNullOrWhiteSpace(commentText))
                {
                    return Json(new { success = false, message = "Comment cannot be empty" });
                }

                if (commentText.Length > 2000)
                {
                    return Json(new { success = false, message = "Comment is too long (max 2000 characters)" });
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("Current UserId: {UserId}", userId);

                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in to comment" });
                }

                var movie = await _context.Movies.FindAsync(movieId);
                if (movie == null)
                {
                    _logger.LogWarning("Movie not found: {MovieId}", movieId);
                    return Json(new { success = false, message = "Movie not found" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return Json(new { success = false, message = "User not found" });
                }

                var comment = new Comment
                {
                    MovieId = movieId,
                    UserId = userId,
                    CommentText = commentText.Trim(),
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comment saved successfully - CommentId: {CommentId}, UserId: {UserId}, MovieId: {MovieId}",
                    comment.Id, userId, movieId);

                return Json(new
                {
                    success = true,
                    message = "Comment posted successfully!",
                    comment = new
                    {
                        id = comment.Id,
                        userName = user.Username,
                        userInitial = user.Username.Substring(0, 1).ToUpper(),
                        commentText = comment.CommentText,
                        timeAgo = "just now",
                        createdAt = comment.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment for MovieId: {MovieId}", movieId);
                return Json(new { success = false, message = "Error submitting comment: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMovieComments(int movieId)
        {
            try
            {
                // ✅ Step 1: Get comments from database
                var commentsFromDb = await _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.MovieId == movieId && c.IsApproved)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                // ✅ Step 2: Transform with REAL-TIME timeAgo calculation
                // This runs in C# memory, so GetTimeAgo() works perfectly!
                var comments = commentsFromDb.Select(c => new
                {
                    id = c.Id,
                    movieId = c.MovieId,
                    userId = c.UserId,
                    userName = c.User != null ? c.User.Username : "Unknown User",
                    userInitial = c.User != null ? c.User.Username.Substring(0, 1).ToUpper() : "?",
                    commentText = c.CommentText,
                    isApproved = c.IsApproved,
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt,
                    timeAgo = GetTimeAgo(c.CreatedAt)  // ✅ Calculates fresh EVERY time!
                }).ToList();

                _logger.LogInformation("Loaded {Count} comments for movie {MovieId}", comments.Count, movieId);

                return Json(new { success = true, comments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comments for movie {MovieId}", movieId);
                return Json(new { success = false, message = "Error loading comments" });
            }
        }

        private string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;  // ✅ Uses CURRENT time every call!

            if (timeSpan.TotalMinutes < 1)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) != 1 ? "s" : "")} ago";

            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) != 1 ? "s" : "")} ago";
        }

        public IActionResult Search(string searchTerm)
        {
            return View("SearchResults", searchTerm);
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult UserDashboard()
        {
            _logger.LogInformation("UserDashboard accessed by {User}", User.Identity?.Name ?? "Unknown");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SavePreferences([FromBody] List<string> genres)
        {
            try
            {
                if (genres == null || !genres.Any())
                {
                    return Json(new { success = false, message = "No genres selected" });
                }

                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    HttpContext.Session.SetString("UserGenrePreferences", JsonSerializer.Serialize(genres));
                    _logger.LogInformation("Preferences saved to session (no user ID): {Genres}", string.Join(", ", genres));
                    return Json(new { success = true, message = "Preferences saved successfully" });
                }

                // Remove existing preferences
                var existingPreferences = await _context.UserPreferences
                    .Where(up => up.UserId == userId)
                    .ToListAsync();

                if (existingPreferences.Any())
                {
                    _context.UserPreferences.RemoveRange(existingPreferences);
                }

                // Add new preferences
                foreach (var genre in genres)
                {
                    _context.UserPreferences.Add(new UserPreference
                    {
                        UserId = userId,
                        GenreName = genre,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                // Save to session for immediate availability
                HttpContext.Session.SetString("UserGenrePreferences", JsonSerializer.Serialize(genres));

                _logger.LogInformation("User {UserId} preferences saved: {Genres}",
                    userId, string.Join(", ", genres));

                return Json(new { success = true, message = "Preferences saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user preferences");
                return Json(new { success = false, message = "Error saving preferences: " + ex.Message });
            }
        }

        // ========================================
        // REPLACE YOUR ENTIRE Home ACTION WITH THIS
        // In AdvisorController.cs
        // ========================================

        public async Task<IActionResult> Home(int page = 1, int pageSize = 20, bool hideWatched = true)
        {
            var model = new UserDashboardViewModel
            {
                Movie = new List<Movie_Advisor.Models.Movie>()
            };

            try
            {
                var userId = GetCurrentUserId();
                List<string>? userPreferences = null;

                // Get preferences from session
                var preferencesJson = HttpContext.Session.GetString("UserGenrePreferences");

                if (!string.IsNullOrEmpty(preferencesJson))
                {
                    userPreferences = JsonSerializer.Deserialize<List<string>>(preferencesJson);
                    _logger.LogInformation("Loaded preferences from session: {Prefs}",
                        string.Join(", ", userPreferences));
                }
                // If not in session, load from database
                else if (userId > 0)
                {
                    userPreferences = await _context.UserPreferences
                        .Where(up => up.UserId == userId)
                        .Select(up => up.GenreName)
                        .ToListAsync();

                    if (userPreferences.Any())
                    {
                        HttpContext.Session.SetString("UserGenrePreferences",
                            JsonSerializer.Serialize(userPreferences));
                        _logger.LogInformation("Loaded preferences from database: {Prefs}",
                            string.Join(", ", userPreferences));
                    }
                }

                // ✅ NEW: Get watched movie IDs for filtering
                var watchedMovieIds = new List<int>();
                if (userId > 0 && hideWatched)
                {
                    watchedMovieIds = await _context.WatchedMovies
                        .Where(w => w.UserId == userId)
                        .Select(w => w.MovieId)
                        .ToListAsync();

                    _logger.LogInformation("User {UserId} has watched {Count} movies", userId, watchedMovieIds.Count);
                }

                // Start with all movies query
                IQueryable<Movie_Advisor.Models.Movie> moviesQuery = _context.Movies;

                // ✅ NEW: Filter out watched movies
                if (watchedMovieIds.Any())
                {
                    moviesQuery = moviesQuery.Where(m => !watchedMovieIds.Contains(m.Id));
                }

                // Filter by user preferences using Genre string column
                if (userPreferences != null && userPreferences.Any())
                {
                    // Convert to in-memory for complex string operations
                    var allMovies = await moviesQuery.ToListAsync();

                    var filteredMovies = allMovies.Where(m =>
                    {
                        if (string.IsNullOrEmpty(m.Genre)) return false;

                        var movieGenres = m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(g => g.Trim().ToLower());

                        return userPreferences.Any(pref =>
                            movieGenres.Contains(pref.ToLower())
                        );
                    }).ToList();

                    ViewBag.FilteredByPreferences = true;
                    ViewBag.UserPreferences = userPreferences;
                    ViewBag.PreferenceCount = userPreferences.Count;

                    // Calculate pagination
                    var totalMovies = filteredMovies.Count;
                    var totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

                    ViewBag.CurrentPage = page;
                    ViewBag.TotalPages = totalPages;
                    ViewBag.TotalMovies = totalMovies;
                    ViewBag.HideWatched = hideWatched;

                    model.Movie = filteredMovies
                        .OrderByDescending(m => m.ReleaseDate)
                        .ThenBy(m => m.Title)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    _logger.LogInformation("Filtered {Count} movies from {Total} based on preferences",
                        totalMovies, allMovies.Count);
                }
                else
                {
                    // No preferences - show all movies (minus watched if filtered)
                    ViewBag.FilteredByPreferences = false;

                    var totalMovies = await moviesQuery.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

                    ViewBag.CurrentPage = page;
                    ViewBag.TotalPages = totalPages;
                    ViewBag.TotalMovies = totalMovies;
                    ViewBag.HideWatched = hideWatched;

                    model.Movie = await moviesQuery
                        .OrderByDescending(m => m.ReleaseDate)
                        .ThenBy(m => m.Title)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                }

                _logger.LogInformation("Loaded {Count} movies for page {Page}", model.Movie.Count, page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movies for Home page");
                ViewBag.ErrorMessage = "Error loading movies. Please try again.";
            }

            return View(model);
        }
        public async Task<IActionResult> FilterByGenre(string genre, int page = 1, int pageSize = 20, bool hideWatched = true)
        {
            _logger.LogInformation("Filtering movies by genre: {Genre}", genre);
            var model = new UserDashboardViewModel
            {
                Movie = new List<Movie_Advisor.Models.Movie>()
            };

            try
            {
                var userId = GetCurrentUserId();

                // ✅ NEW: Get watched movie IDs for filtering
                var watchedMovieIds = new List<int>();
                if (userId > 0 && hideWatched)
                {
                    watchedMovieIds = await _context.WatchedMovies
                        .Where(w => w.UserId == userId)
                        .Select(w => w.MovieId)
                        .ToListAsync();

                    _logger.LogInformation("User {UserId} has watched {Count} movies", userId, watchedMovieIds.Count);
                }

                var allMovies = await _context.Movies.ToListAsync();

                // ✅ NEW: Filter out watched movies first
                if (watchedMovieIds.Any())
                {
                    allMovies = allMovies.Where(m => !watchedMovieIds.Contains(m.Id)).ToList();
                }

                // Then filter by genre
                var filteredMovies = allMovies.Where(m =>
                    !string.IsNullOrEmpty(m.Genre) &&
                    m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(g => g.Trim())
                           .Any(g => g.Equals(genre, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                var totalMovies = filteredMovies.Count;
                var totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalMovies = totalMovies;
                ViewBag.SelectedGenre = genre;
                ViewBag.FilteredByPreferences = false;
                ViewBag.HideWatched = hideWatched;

                model.Movie = filteredMovies
                    .OrderByDescending(m => m.ReleaseDate)
                    .ThenBy(m => m.Title)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Found {Count} movies for genre {Genre}", model.Movie.Count, genre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering movies by genre: {Genre}", genre);
                ViewBag.ErrorMessage = "Error loading movies. Please try again.";
            }

            return View("Home", model);
        }
        [HttpGet]
        public IActionResult ClearPreferences()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }

                // Clear user preferences from database
                var userPreferences = _context.UserPreferences
                    .Where(up => up.UserId == userId.Value)
                    .ToList();

                _context.UserPreferences.RemoveRange(userPreferences);
                _context.SaveChanges();

                TempData["Success"] = "Preferences cleared! Showing all movies.";

                return RedirectToAction("Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing preferences");
                TempData["Error"] = "Failed to clear preferences.";
                return RedirectToAction("Home");
            }
        }

        public IActionResult AllMovies(int page = 1)
        {
            HttpContext.Session.Remove("UserGenrePreferences");
            return RedirectToAction("Home", new { page });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            _logger.LogInformation("AdminDashboard accessed by {Admin}", User.Identity?.Name ?? "Unknown");

            try
            {
                var model = new AdminDashboardViewModel
                {
                    TotalMovies = _context.Movies.Count(),
                    TotalUsers = _context.Users.Count(),
                    RecentMovies = _context.Movies
                        .OrderByDescending(m => m.CreatedAt)
                        .Take(5)
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");

                var model = new AdminDashboardViewModel
                {
                    TotalMovies = 0,
                    TotalUsers = 0,
                    RecentMovies = new List<Movie_Advisor.Models.Movie>()
                };

                ViewBag.ErrorMessage = "Error loading dashboard data. Please try again.";
                return View(model);
            }
        }

        public async Task<IActionResult> Genre()
        {
            try
            {
                // Get all unique genres from movies
                var allMovies = await _context.Movies
                    .Where(m => !string.IsNullOrEmpty(m.Genre))
                    .ToListAsync();

                var genres = allMovies
                    .SelectMany(m => m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim()))
                    .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new ViewGenre
                    {
                        Name = g.Key,
                        MovieCount = g.Count()
                    })
                    .OrderBy(g => g.Name)
                    .ToList();

                ViewBag.Genres = genres;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading genres");
                ViewBag.Genres = new List<ViewGenre>();
            }

            return View();
        }

        public IActionResult FilterByCountry(string country)
        {
            _logger.LogInformation("Filtering movies by country: {Country}", country);
            ViewBag.SelectedCountry = country;
            var model = new UserDashboardViewModel
            {
                Movie = new List<Movie_Advisor.Models.Movie>()
            };
            return View("Country", model);
        }

        public async Task<IActionResult> Movie(string searchQuery, string selectedGenre, int? selectedYear, string sortBy, int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Movie action - Genre: {Genre}, Year: {Year}, Sort: {Sort}, Search: {Search}",
                    selectedGenre, selectedYear, sortBy, searchQuery);

                var model = new UserDashboardViewModel
                {
                    Movie = new List<Movie_Advisor.Models.Movie>(),
                    Genres = new List<ViewGenre>(),
                    SearchQuery = searchQuery,
                    SelectedGenre = selectedGenre,
                    SelectedYear = selectedYear,
                    SortBy = sortBy,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                // Get all genres
                var allMovies = await _context.Movies
                    .Where(m => !string.IsNullOrEmpty(m.Genre))
                    .ToListAsync();

                var genres = allMovies
                    .SelectMany(m => m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim()))
                    .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new ViewGenre
                    {
                        Name = g.Key,
                        MovieCount = g.Count()
                    })
                    .OrderBy(g => g.Name)
                    .ToList();

                model.Genres = genres;

                // Get all movies for filtering
                var moviesQuery = await _context.Movies.ToListAsync();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    moviesQuery = moviesQuery.Where(m =>
                        m.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        (m.Description != null && m.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                        (m.Genre != null && m.Genre.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Apply genre filter
                if (!string.IsNullOrWhiteSpace(selectedGenre))
                {
                    moviesQuery = moviesQuery.Where(m =>
                        !string.IsNullOrEmpty(m.Genre) &&
                        m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(g => g.Trim())
                               .Any(g => g.Equals(selectedGenre, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Apply year filter
                if (selectedYear.HasValue)
                {
                    moviesQuery = moviesQuery.Where(m =>
                        m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year == selectedYear.Value
                    ).ToList();
                }

                // Apply sorting
                moviesQuery = sortBy switch
                {
                    "rating" => moviesQuery.OrderByDescending(m => m.Rating ?? 0).ToList(),
                    "year" => moviesQuery.OrderByDescending(m => m.ReleaseDate).ToList(),
                    "title" => moviesQuery.OrderBy(m => m.Title).ToList(),
                    _ => moviesQuery.OrderByDescending(m => m.CreatedAt).ToList()
                };

                // Get total count
                var totalMovies = moviesQuery.Count;
                model.TotalMovies = totalMovies;
                model.TotalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

                // Ensure page is within valid range
                if (page < 1) page = 1;
                if (page > model.TotalPages && model.TotalPages > 0) page = model.TotalPages;
                model.CurrentPage = page;

                // Apply pagination
                model.Movie = moviesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Loaded {Count} movies for page {Page} of {TotalPages}",
                    model.Movie.Count, page, model.TotalPages);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movies for Movie page");

                var errorModel = new UserDashboardViewModel
                {
                    Movie = new List<Movie_Advisor.Models.Movie>(),
                    Genres = new List<ViewGenre>(),
                    CurrentPage = 1,
                    TotalPages = 0
                };

                ViewBag.ErrorMessage = "Error loading movies. Please try again later.";
                return View(errorModel);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> NewMovie()
        {
            try
            {
                // Get all available genres from database
                var allMovies = await _context.Movies
                    .Where(m => !string.IsNullOrEmpty(m.Genre))
                    .ToListAsync();

                var genres = allMovies
                    .SelectMany(m => m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim()))
                    .Distinct()
                    .OrderBy(g => g)
                    .ToList();

                // If no genres in movies, use default list
                if (!genres.Any())
                {
                    genres = new List<string>
                    {
                        "Action", "Adventure", "Animation", "Biography", "Comedy",
                        "Crime", "Documentary", "Drama", "Family", "Fantasy",
                        "Horror", "Musical", "Mystery", "Romance", "Sci-Fi",
                        "Thriller", "War", "Western"
                    };
                }

                ViewBag.AvailableGenres = genres;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading New Movie page");
                ViewBag.ErrorMessage = "Error loading page. Please try again.";
                return View();
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMovie(Movie movie, List<string> selectedGenres)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(movie.Title))
                {
                    TempData["Error"] = "Movie title is required.";
                    return RedirectToAction("NewMovie");
                }

                // Combine selected genres into comma-separated string
                if (selectedGenres != null && selectedGenres.Any())
                {
                    movie.Genre = string.Join(", ", selectedGenres);
                }

                // Set creation date
                movie.CreatedAt = DateTime.UtcNow;

                // Add to database
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin added new movie: {Title} (ID: {Id})", movie.Title, movie.Id);
                TempData["Success"] = $"Movie '{movie.Title}' added successfully!";

                return RedirectToAction("ManageMovies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating movie");
                TempData["Error"] = "Error adding movie. Please try again.";
                return RedirectToAction("NewMovie");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageMovies(int page = 1, int pageSize = 20, string search = "")
        {
            try
            {
                var moviesQuery = _context.Movies.AsQueryable();

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    moviesQuery = moviesQuery.Where(m =>
                        m.Title.Contains(search) ||
                        (m.Genre != null && m.Genre.Contains(search))
                    );
                }

                var totalMovies = await moviesQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

                var movies = await moviesQuery
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalMovies = totalMovies;
                ViewBag.SearchQuery = search;

                return View(movies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Manage Movies page");
                ViewBag.ErrorMessage = "Error loading movies. Please try again.";
                return View(new List<Movie>());
            }
        }

        // Add to AdvisorController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in to delete comments" });
                }

                var comment = await _context.Comments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    return Json(new { success = false, message = "Comment not found" });
                }

                // Check if user owns the comment OR is an admin
                var isAdmin = User.IsInRole("Admin");
                if (comment.UserId != userId && !isAdmin)
                {
                    return Json(new { success = false, message = "You can only delete your own comments" });
                }

                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted comment {CommentId}", userId, commentId);

                return Json(new
                {
                    success = true,
                    message = "Comment deleted successfully",
                    commentId = commentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return Json(new { success = false, message = "Error deleting comment" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment([FromForm] int commentId, [FromForm] string commentText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(commentText))
                {
                    return Json(new { success = false, message = "Comment cannot be empty" });
                }

                if (commentText.Length > 2000)
                {
                    return Json(new { success = false, message = "Comment is too long (max 2000 characters)" });
                }

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in to edit comments" });
                }

                var comment = await _context.Comments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    return Json(new { success = false, message = "Comment not found" });
                }

                // Check if user owns the comment
                if (comment.UserId != userId)
                {
                    return Json(new { success = false, message = "You can only edit your own comments" });
                }

                // Check if comment is less than 15 minutes old (optional)
                var timeSinceCreation = DateTime.UtcNow - comment.CreatedAt;
                if (timeSinceCreation.TotalMinutes > 15)
                {
                    return Json(new { success = false, message = "Comments can only be edited within 15 minutes of posting" });
                }

                comment.CommentText = commentText.Trim();
                comment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} edited comment {CommentId}", userId, commentId);

                return Json(new
                {
                    success = true,
                    message = "Comment updated successfully",
                    comment = new
                    {
                        id = comment.Id,
                        commentText = comment.CommentText,
                        updatedAt = comment.UpdatedAt,
                        isEdited = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing comment {CommentId}", commentId);
                return Json(new { success = false, message = "Error updating comment" });
            }
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditMovie(int id)
        {
            try
            {
                var movie = await _context.Movies.FindAsync(id);

                if (movie == null)
                {
                    TempData["Error"] = "Movie not found.";
                    return RedirectToAction("ManageMovies");
                }

                // Get available genres
                var allMovies = await _context.Movies
                    .Where(m => !string.IsNullOrEmpty(m.Genre))
                    .ToListAsync();

                var genres = allMovies
                    .SelectMany(m => m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim()))
                    .Distinct()
                    .OrderBy(g => g)
                    .ToList();

                if (!genres.Any())
                {
                    genres = new List<string>
                    {
                        "Action", "Adventure", "Animation", "Biography", "Comedy",
                        "Crime", "Documentary", "Drama", "Family", "Fantasy",
                        "Horror", "Musical", "Mystery", "Romance", "Sci-Fi",
                        "Thriller", "War", "Western"
                    };
                }

                ViewBag.AvailableGenres = genres;
                ViewBag.SelectedGenres = movie.Genre?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(g => g.Trim())
                                                    .ToList() ?? new List<string>();

                return View(movie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit Movie page for ID: {Id}", id);
                TempData["Error"] = "Error loading movie. Please try again.";
                return RedirectToAction("ManageMovies");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMovie(Movie movie, List<string> selectedGenres)
        {
            try
            {
                var existingMovie = await _context.Movies.FindAsync(movie.Id);

                if (existingMovie == null)
                {
                    TempData["Error"] = "Movie not found.";
                    return RedirectToAction("ManageMovies");
                }

                // Update properties
                existingMovie.Title = movie.Title;
                existingMovie.Description = movie.Description;
                existingMovie.ReleaseDate = movie.ReleaseDate;
                existingMovie.Duration = movie.Duration;
                existingMovie.Rating = movie.Rating;
                existingMovie.Language = movie.Language;
                existingMovie.Country = movie.Country;
                existingMovie.PosterUrl = movie.PosterUrl;
                existingMovie.BackdropUrl = movie.BackdropUrl;
                existingMovie.TrailerUrl = movie.TrailerUrl;
                existingMovie.ImdbId = movie.ImdbId;
                existingMovie.Budget = movie.Budget;
                existingMovie.Revenue = movie.Revenue;

                // Update genres
                if (selectedGenres != null && selectedGenres.Any())
                {
                    existingMovie.Genre = string.Join(", ", selectedGenres);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin updated movie: {Title} (ID: {Id})", movie.Title, movie.Id);
                TempData["Success"] = $"Movie '{movie.Title}' updated successfully!";

                return RedirectToAction("ManageMovies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie ID: {Id}", movie.Id);
                TempData["Error"] = "Error updating movie. Please try again.";
                return RedirectToAction("EditMovie", new { id = movie.Id });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            try
            {
                var movie = await _context.Movies.FindAsync(id);

                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found." });
                }

                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin deleted movie: {Title} (ID: {Id})", movie.Title, id);

                return Json(new { success = true, message = $"Movie '{movie.Title}' deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie ID: {Id}", id);
                return Json(new { success = false, message = "Error deleting movie. Please try again." });
            }
        }

        public IActionResult TopImdb()
        {
            return View();
        }

        public IActionResult Watchlist()
        {
            return View();
        }

        public IActionResult WatchHistory()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetWatchlistMovies()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in" });
                }

                var movies = await _context.Watchlists
                    .Include(w => w.Movie)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.AddedAt)
                    .Select(w => new
                    {
                        id = w.Movie!.Id,
                        title = w.Movie.Title,
                        description = w.Movie.Description,
                        genre = w.Movie.Genre,
                        duration = w.Movie.Duration,
                        releaseDate = w.Movie.ReleaseDate,
                        rating = w.Movie.Rating,
                        posterUrl = w.Movie.PosterUrl,
                        backdropUrl = w.Movie.BackdropUrl,
                        trailerUrl = w.Movie.TrailerUrl
                    })
                    .ToListAsync();

                return Json(new { success = true, movies = movies });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching watchlist movies");
                return Json(new { success = false, message = "Error loading watchlist" });
            }
        }
        public async Task<IActionResult> MovieDetail(int id)
        {
            try
            {
                _logger.LogInformation("Loading movie details for ID: {Id}", id);

                var movie = await _context.Movies.FindAsync(id);

                if (movie == null)
                {
                    _logger.LogWarning("Movie with ID {Id} not found", id);
                    TempData["Error"] = "Movie not found.";
                    return RedirectToAction("Home");
                }

                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                var viewModel = new MovieDetailsViewModel
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Tagline = null,
                    Description = movie.Description,
                    Genre = movie.Genre,
                    Duration = movie.Duration ?? 0,
                    ReleaseYear = movie.ReleaseDate?.Year ?? 0,
                    Rating = null,
                    IMDBRating = movie.Rating ?? 0m,
                    UserRating = 0m,
                    TotalRatings = "0",
                    PosterUrl = movie.PosterUrl,
                    BackdropUrl = movie.BackdropUrl,
                    TrailerKey = movie.TrailerUrl,
                    Director = null,
                    Writer = null,
                    Country = movie.Country,
                    Language = movie.Language,
                    Budget = movie.Budget.HasValue ? $"${movie.Budget.Value:N0}" : null,
                    BoxOffice = movie.Revenue.HasValue ? $"${movie.Revenue.Value:N0}" : null,
                    UserHasRated = false,
                    UserRatingValue = 0,

                    CurrentUserId = currentUserId,
                    IsAdmin = isAdmin
                };

                _logger.LogInformation("Movie details loaded successfully for: {Title}", movie.Title);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movie details for ID: {Id}", id);
                TempData["Error"] = "Error loading movie details. Please try again.";
                return RedirectToAction("Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTrailer(int id)
        {
            try
            {
                var movie = await _context.Movies.FindAsync(id);

                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found" });
                }

                if (!string.IsNullOrEmpty(movie.TrailerUrl))
                {
                    _logger.LogInformation("Using stored trailer for movie {Title}: {VideoId}",
                        movie.Title, movie.TrailerUrl);
                    return Json(new
                    {
                        success = true,
                        videoId = movie.TrailerUrl
                    });
                }

                _logger.LogInformation("Searching YouTube for trailer: {Title}", movie.Title);

                var searchQuery = movie.ReleaseDate.HasValue
                    ? $"{movie.Title} {movie.ReleaseDate.Value.Year}"
                    : movie.Title;

                var trailers = await _youtubeService.SearchMovieTrailerAsync(searchQuery, 1);

                if (trailers != null && trailers.Any())
                {
                    var videoId = trailers.First().VideoId;

                    // Save it to database for next time
                    movie.TrailerUrl = videoId;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Found and saved trailer for {Title}: {VideoId}",
                        movie.Title, videoId);

                    return Json(new
                    {
                        success = true,
                        videoId = videoId
                    });
                }

                _logger.LogWarning("No trailer found for {Title}", movie.Title);
                return Json(new
                {
                    success = false,
                    message = "Trailer not found on YouTube"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trailer for movie {Id}", id);
                return Json(new
                {
                    success = false,
                    message = "Error fetching trailer: " + ex.Message
                });
            }
        }

        // BONUS: Bulk update trailers for all movies (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAllTrailers()
        {
            try
            {
                var moviesWithoutTrailers = await _context.Movies
                    .Where(m => string.IsNullOrEmpty(m.TrailerUrl))
                    .Take(50) // Do 50 at a time to avoid rate limits
                    .ToListAsync();

                int updated = 0;
                int failed = 0;

                foreach (var movie in moviesWithoutTrailers)
                {
                    try
                    {
                        var searchQuery = movie.ReleaseDate.HasValue
                            ? $"{movie.Title} {movie.ReleaseDate.Value.Year}"
                            : movie.Title;

                        var trailers = await _youtubeService.SearchMovieTrailerAsync(searchQuery, 1);

                        if (trailers != null && trailers.Any())
                        {
                            movie.TrailerUrl = trailers.First().VideoId;
                            updated++;

                            _logger.LogInformation("Updated trailer for {Title}", movie.Title);
                        }
                        else
                        {
                            failed++;
                            _logger.LogWarning("No trailer found for {Title}", movie.Title);
                        }

                        // Delay to respect YouTube API rate limits (100 requests per 100 seconds)
                        await Task.Delay(1000); // 1 request per second = safe
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogError(ex, "Error updating trailer for {Title}", movie.Title);
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Updated {updated} trailers, {failed} failed",
                    updated = updated,
                    failed = failed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk trailer update");
                return Json(new
                {
                    success = false,
                    message = "Error updating trailers: " + ex.Message
                });
            }
        }

        public IActionResult SearchResult()
        {
            return View();
        }

        // Debug action to view database
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DebugDatabase()
        {
            try
            {
                var sampleMovies = await _context.Movies
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(10)
                    .Select(m => new
                    {
                        m.Id,
                        m.TmdbId,
                        m.Title,
                        m.Genre,
                        GenreList = !string.IsNullOrEmpty(m.Genre)
                            ? m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(g => g.Trim()).ToList()
                            : new List<string>(),
                        m.ReleaseDate,
                        m.Rating,
                        m.CreatedAt
                    })
                    .ToListAsync();

                var allMovies = await _context.Movies
                    .Where(m => !string.IsNullOrEmpty(m.Genre))
                    .ToListAsync();

                var allGenres = allMovies
                    .SelectMany(m => m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim()))
                    .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new
                    {
                        Name = g.Key,
                        MovieCount = g.Count()
                    })
                    .OrderBy(g => g.Name)
                    .ToList();

                var preferences = await _context.UserPreferences
                    .Select(up => new
                    {
                        up.UserId,
                        up.GenreName,
                        up.CreatedAt
                    })
                    .ToListAsync();

                var debugInfo = new
                {
                    TotalMovies = await _context.Movies.CountAsync(),
                    TotalMoviesWithGenres = allMovies.Count,
                    TotalGenres = allGenres.Count,
                    TotalUserPreferences = preferences.Count,
                    SampleMovies = sampleMovies,
                    AllGenres = allGenres,
                    UserPreferences = preferences
                };

                return Json(debugInfo);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            return sessionUserId ?? 0;
        }

        // Add these actions to your AdvisorController.cs

        [HttpGet]
        public async Task<IActionResult> TopIMDB(string type = "top250", string decade = null, string genre = null, string award = null, int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("TopIMDB - Type: {Type}, Decade: {Decade}, Genre: {Genre}, Award: {Award}, Page: {Page}",
                    type, decade, genre, award, page);

                var model = new UserDashboardViewModel
                {
                    Movie = new List<Movie_Advisor.Models.Movie>()
                };

                // Start with all movies
                var moviesQuery = await _context.Movies.ToListAsync();

                // Filter by IMDB rating (only show highly rated movies)
                moviesQuery = moviesQuery.Where(m => m.Rating >= 7.0m).ToList();

                // Apply genre filter
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    moviesQuery = moviesQuery.Where(m =>
                        !string.IsNullOrEmpty(m.Genre) &&
                        m.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(g => g.Trim().ToLower())
                               .Any(g => g.Contains(genre.ToLower()))
                    ).ToList();
                }

                // Apply decade filter
                if (!string.IsNullOrWhiteSpace(decade))
                {
                    moviesQuery = decade.ToLower() switch
                    {
                        "2020s" => moviesQuery.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year >= 2020).ToList(),
                        "2010s" => moviesQuery.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year >= 2010 && m.ReleaseDate.Value.Year < 2020).ToList(),
                        "2000s" => moviesQuery.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year >= 2000 && m.ReleaseDate.Value.Year < 2010).ToList(),
                        "1990s" => moviesQuery.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year >= 1990 && m.ReleaseDate.Value.Year < 2000).ToList(),
                        "1980s" => moviesQuery.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year >= 1980 && m.ReleaseDate.Value.Year < 1990).ToList(),
                        "classics" => moviesQuery.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year < 1980).ToList(),
                        _ => moviesQuery
                    };
                }

                // Apply award filter (you can customize this based on your database fields)
                if (!string.IsNullOrWhiteSpace(award))
                {
                    // Example: Filter by description containing award keywords
                    moviesQuery = award.ToLower() switch
                    {
                        "oscar" => moviesQuery.Where(m =>
                            m.Description != null &&
                            (m.Description.Contains("Oscar", StringComparison.OrdinalIgnoreCase) ||
                             m.Description.Contains("Academy Award", StringComparison.OrdinalIgnoreCase))
                        ).ToList(),
                        "golden-globe" => moviesQuery.Where(m =>
                            m.Description != null &&
                            m.Description.Contains("Golden Globe", StringComparison.OrdinalIgnoreCase)
                        ).ToList(),
                        "cannes" => moviesQuery.Where(m =>
                            m.Description != null &&
                            m.Description.Contains("Cannes", StringComparison.OrdinalIgnoreCase)
                        ).ToList(),
                        _ => moviesQuery
                    };
                }

                // Sort by rating (highest first)
                moviesQuery = moviesQuery.OrderByDescending(m => m.Rating).ToList();

                // Apply type limit (top250, top100, top50, recent)
                var limitedMovies = type.ToLower() switch
                {
                    "top250" => moviesQuery.Take(250).ToList(),
                    "top100" => moviesQuery.Take(100).ToList(),
                    "top50" => moviesQuery.Take(50).ToList(),
                    "recent" => moviesQuery
                        .Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year >= DateTime.Now.Year - 5)
                        .OrderByDescending(m => m.ReleaseDate)
                        .Take(100)
                        .ToList(),
                    _ => moviesQuery.Take(250).ToList()
                };

                // Calculate pagination
                var totalMovies = limitedMovies.Count;
                var totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

                // Ensure page is within valid range
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                // Apply pagination
                model.Movie = limitedMovies
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Set ViewBag properties
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalMovies = totalMovies;
                ViewBag.FilterType = type;
                ViewBag.SelectedDecade = decade;
                ViewBag.SelectedGenre = genre;
                ViewBag.SelectedAward = award;

                _logger.LogInformation("Loaded {Count} movies for Top IMDB (Type: {Type})", model.Movie.Count, type);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Top IMDB movies");

                var errorModel = new UserDashboardViewModel
                {
                    Movie = new List<Movie_Advisor.Models.Movie>()
                };

                ViewBag.ErrorMessage = "Error loading Top IMDB movies. Please try again.";
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 0;
                ViewBag.TotalMovies = 0;

                return View(errorModel);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWatchlist(int movieId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in to add movies to watchlist" });
                }

                // Check if movie exists
                var movie = await _context.Movies.FindAsync(movieId);
                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found" });
                }

                // Check if already in watchlist
                var existingEntry = await _context.Watchlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId);

                if (existingEntry != null)
                {
                    return Json(new { success = false, message = "Movie already in your watchlist" });
                }

                // Add to watchlist
                var watchlistEntry = new Watchlist
                {
                    UserId = userId,
                    MovieId = movieId,
                    AddedAt = DateTime.UtcNow
                };

                _context.Watchlists.Add(watchlistEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} added movie {MovieId} to watchlist", userId, movieId);

                return Json(new { success = true, message = "Added to watchlist!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie {MovieId} to watchlist", movieId);
                return Json(new { success = false, message = "Error adding to watchlist" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWatchlist(int movieId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in" });
                }

                var watchlistEntry = await _context.Watchlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId);

                if (watchlistEntry == null)
                {
                    return Json(new { success = false, message = "Movie not in watchlist" });
                }

                _context.Watchlists.Remove(watchlistEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} removed movie {MovieId} from watchlist", userId, movieId);

                return Json(new { success = true, message = "Removed from watchlist" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing movie {MovieId} from watchlist", movieId);
                return Json(new { success = false, message = "Error removing from watchlist" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetUserWatchlist()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in" });
                }

                var watchlist = await _context.Watchlists
                    .Include(w => w.Movie)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.AddedAt)
                    .Select(w => new
                    {
                        id = w.Movie!.Id,
                        title = w.Movie.Title,
                        description = w.Movie.Description,
                        genre = w.Movie.Genre,
                        duration = w.Movie.Duration,
                        releaseDate = w.Movie.ReleaseDate,
                        rating = w.Movie.Rating,
                        posterUrl = w.Movie.PosterUrl,
                        backdropUrl = w.Movie.BackdropUrl,
                        trailerUrl = w.Movie.TrailerUrl,
                        addedAt = w.AddedAt
                    })
                    .ToListAsync();

                return Json(new { success = true, movies = watchlist });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching watchlist for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, message = "Error loading watchlist" });
            }
        }
        // Replace the CheckInWatchlist method in AdvisorController.cs (around line 1592)

        [HttpGet]
        public async Task<IActionResult> CheckInWatchlist(int movieId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { inWatchlist = false });
                }

                // ✅ FIX: Use Count() > 0 instead of AnyAsync() to avoid Oracle True/False issue
                var count = await _context.Watchlists
                    .Where(w => w.UserId == userId && w.MovieId == movieId)
                    .CountAsync();

                var exists = count > 0;

                return Json(new { inWatchlist = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking watchlist status for movie {MovieId}", movieId);
                return Json(new { inWatchlist = false });
            }
        }

        // Mark movie as watched
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsWatched(int movieId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in to mark movies as watched." });
                }

                var movie = await _context.Movies.FindAsync(movieId);
                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found." });
                }

                // Use Count() instead of AnyAsync() for Oracle compatibility
                var count = await _context.WatchedMovies
                    .Where(w => w.UserId == userId && w.MovieId == movieId)
                    .CountAsync();

                if (count > 0)
                {
                    return Json(new { success = false, message = "You've already marked this movie as watched." });
                }

                var watchedMovie = new WatchedMovie
                {
                    UserId = userId,
                    MovieId = movieId,
                    WatchedAt = DateTime.UtcNow
                };

                _context.WatchedMovies.Add(watchedMovie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} marked movie {MovieId} as watched", userId, movieId);

                return Json(new { success = true, message = "Movie marked as watched!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking movie {MovieId} as watched", movieId);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        // Remove from watched list
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWatched(int movieId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in." });
                }

                var watchedMovie = await _context.WatchedMovies
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId);

                if (watchedMovie == null)
                {
                    return Json(new { success = false, message = "Movie not found in your watched list." });
                }

                _context.WatchedMovies.Remove(watchedMovie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} removed movie {MovieId} from watched", userId, movieId);

                return Json(new { success = true, message = "Removed from watched list." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing movie {MovieId} from watched", movieId);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        // Check if movie is watched by current user
        [HttpGet]
        public async Task<IActionResult> IsWatched(int movieId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { isWatched = false });
                }

                var count = await _context.WatchedMovies
                    .Where(w => w.UserId == userId && w.MovieId == movieId)
                    .CountAsync();

                return Json(new { isWatched = count > 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking watched status for movie {MovieId}", movieId);
                return Json(new { isWatched = false });
            }
        }

        // Get user's watched movies (optional - for a "My Watched Movies" page)
        [HttpGet]
        public async Task<IActionResult> MyWatchedMovies()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "User");
                }

                var watchedMovies = await _context.WatchedMovies
                    .Include(w => w.Movie)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.WatchedAt)
                    .Select(w => new
                    {
                        id = w.Movie!.Id,
                        title = w.Movie.Title,
                        description = w.Movie.Description,
                        genre = w.Movie.Genre,
                        duration = w.Movie.Duration,
                        releaseDate = w.Movie.ReleaseDate,
                        rating = w.Movie.Rating,
                        posterUrl = w.Movie.PosterUrl,
                        backdropUrl = w.Movie.BackdropUrl,
                        watchedAt = w.WatchedAt
                    })
                    .ToListAsync();

                return Json(new { success = true, movies = watchedMovies });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching watched movies for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, message = "Error loading watched movies." });
            }
        }

        // Add these methods to your AdvisorController.cs

        // Add these methods to your AdvisorController.cs

        [HttpGet]
        public async Task<IActionResult> WatchHistory(int page = 1, int pageSize = 20, string sortBy = "recent")
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    TempData["Error"] = "Please log in to view your watch history.";
                    return RedirectToAction("Login", "User");
                }

                // Get all watched movies for the user
                var watchedMoviesQuery = _context.WatchedMovies
                    .Include(w => w.Movie)
                    .Where(w => w.UserId == userId)
                    .AsQueryable();

                // Apply sorting
                watchedMoviesQuery = sortBy switch
                {
                    "oldest" => watchedMoviesQuery.OrderBy(w => w.WatchedAt),
                    "title" => watchedMoviesQuery.OrderBy(w => w.Movie!.Title),
                    "rating" => watchedMoviesQuery.OrderByDescending(w => w.Movie!.Rating),
                    _ => watchedMoviesQuery.OrderByDescending(w => w.WatchedAt) // recent (default)
                };

                // Get total count
                var totalMovies = await watchedMoviesQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

                // Ensure page is within valid range
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                // Get paginated results
                var watchedMovies = await watchedMoviesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(w => new WatchHistoryItemViewModel
                    {
                        WatchedId = w.Id, // Changed from w.WatchedId to w.Id
                        MovieId = w.Movie!.Id,
                        Title = w.Movie.Title,
                        Description = w.Movie.Description,
                        Genre = w.Movie.Genre,
                        Duration = w.Movie.Duration,
                        ReleaseDate = w.Movie.ReleaseDate,
                        Rating = w.Movie.Rating,
                        PosterUrl = w.Movie.PosterUrl,
                        BackdropUrl = w.Movie.BackdropUrl,
                        TrailerUrl = w.Movie.TrailerUrl,
                        WatchedAt = w.WatchedAt,
                        WatchedAtFormatted = w.WatchedAt.ToString("MMM dd, yyyy")
                    })
                    .ToListAsync();

                var model = new WatchHistoryViewModel
                {
                    WatchedMovies = watchedMovies,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalMovies = totalMovies,
                    PageSize = pageSize,
                    SortBy = sortBy
                };

                _logger.LogInformation("User {UserId} loaded watch history - {Count} movies on page {Page}",
                    userId, watchedMovies.Count, page);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading watch history for user {UserId}", GetCurrentUserId());
                TempData["Error"] = "Error loading watch history. Please try again.";

                var errorModel = new WatchHistoryViewModel
                {
                    WatchedMovies = new List<WatchHistoryItemViewModel>(),
                    CurrentPage = 1,
                    TotalPages = 0,
                    TotalMovies = 0
                };

                return View(errorModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWatchHistory(int watchedId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in." });
                }

                var watchedMovie = await _context.WatchedMovies
                    .FirstOrDefaultAsync(w => w.Id == watchedId && w.UserId == userId); // Changed from w.WatchedId to w.Id

                if (watchedMovie == null)
                {
                    return Json(new { success = false, message = "Movie not found in your watch history." });
                }

                _context.WatchedMovies.Remove(watchedMovie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} removed watchedId {WatchedId} from history", userId, watchedId);

                return Json(new { success = true, message = "Removed from watch history." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing watchedId {WatchedId} from history", watchedId);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearWatchHistory()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in." });
                }

                var watchedMovies = await _context.WatchedMovies
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                if (!watchedMovies.Any())
                {
                    return Json(new { success = false, message = "Your watch history is already empty." });
                }

                _context.WatchedMovies.RemoveRange(watchedMovies);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} cleared their entire watch history ({Count} movies)",
                    userId, watchedMovies.Count);

                return Json(new { success = true, message = "Watch history cleared successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing watch history for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWatchHistoryStats()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in." });
                }

                var watchedMovies = await _context.WatchedMovies
                    .Include(w => w.Movie)
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                var totalMovies = watchedMovies.Count;
                var totalMinutes = watchedMovies.Sum(w => w.Movie?.Duration ?? 0);
                var totalHours = Math.Round(totalMinutes / 60.0, 1);

                var genreCounts = watchedMovies
                    .Where(w => !string.IsNullOrEmpty(w.Movie?.Genre))
                    .SelectMany(w => w.Movie!.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim()))
                    .GroupBy(g => g)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new { genre = g.Key, count = g.Count() })
                    .ToList();

                return Json(new
                {
                    success = true,
                    stats = new
                    {
                        totalMovies,
                        totalHours,
                        totalMinutes,
                        favoriteGenres = genreCounts
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting watch history stats for user {UserId}", GetCurrentUserId());
                return Json(new { success = false, message = "Error loading statistics." });
            }
        }
        // Helper method to get rank for a movie in Top IMDB
        private int GetMovieRank(int movieId, List<Movie_Advisor.Models.Movie> rankedMovies)
        {
            return rankedMovies.FindIndex(m => m.Id == movieId) + 1;
        }
    }
}