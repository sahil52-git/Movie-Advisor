using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movie_Advisor.Data;
using Movie_Advisor.Models;
using Movie_Advisor.ViewModels;
using System.Security.Claims;

namespace Movie_Advisor.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MovieController> _logger;

        public MovieController(ApplicationDbContext context, ILogger<MovieController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Movies List
        public async Task<IActionResult> Index(string searchTerm, string genre, int page = 1, int pageSize = 20, bool hideWatched = false)
        {
            try
            {
                var query = _context.Movies.AsQueryable();

                // ✅ NEW: Get watched movie IDs if hideWatched is enabled
                var watchedMovieIds = new List<int>();
                if (hideWatched)
                {
                    var userId = GetCurrentUserId();
                    if (userId > 0)
                    {
                        watchedMovieIds = await _context.WatchedMovies
                            .Where(w => w.UserId == userId)
                            .Select(w => w.MovieId)
                            .ToListAsync();

                        _logger.LogInformation("Admin {UserId} has watched {Count} movies", userId, watchedMovieIds.Count);
                    }
                }

                // ✅ NEW: Filter out watched movies
                if (watchedMovieIds.Any())
                {
                    query = query.Where(m => !watchedMovieIds.Contains(m.Id));
                }

                // Search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(m =>
                        m.Title.Contains(searchTerm) ||
                        (m.Description != null && m.Description.Contains(searchTerm)) ||
                        (m.Genre != null && m.Genre.Contains(searchTerm))
                    );
                }

                // Genre filter
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    query = query.Where(m => m.Genre != null && m.Genre.Contains(genre));
                }

                // Get total count for pagination
                var totalMovies = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);

                // Get paginated movies
                var movies = await query
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var model = new MovieManagementViewModel
                {
                    Movies = movies,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalMovies = totalMovies,
                    SearchTerm = searchTerm,
                    SelectedGenre = genre,
                    PageSize = pageSize
                };

                // ✅ NEW: Pass hideWatched to view
                ViewBag.HideWatched = hideWatched;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movies for admin panel");
                return View(new MovieManagementViewModel { Movies = new List<Movie>() });
            }
        }

        // GET: Movie Details (for editing)
        [HttpGet]
        public async Task<IActionResult> GetMovie(int id)
        {
            try
            {
                var movie = await _context.Movies.FindAsync(id);
                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found" });
                }
                return Json(new { success = true, movie });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie {Id}", id);
                return Json(new { success = false, message = "Error loading movie" });
            }
        }

        // POST: Create Movie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] MovieFormData data)
        {
            try
            {
                var movie = new Movie
                {
                    Title = data.Title,
                    Description = data.Description,
                    Genre = data.Genre,
                    ReleaseDate = data.ReleaseDate,
                    Duration = data.Duration,
                    Rating = data.Rating,
                    PosterUrl = data.PosterUrl,
                    BackdropUrl = data.BackdropUrl,
                    TrailerUrl = data.TrailerUrl,
                    Language = data.Language,
                    Country = data.Country,
                    ImdbId = data.ImdbId,
                    TmdbId = data.TmdbId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Movie created: {Title} (ID: {Id})", movie.Title, movie.Id);
                return Json(new { success = true, message = "Movie added successfully!", movieId = movie.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating movie");
                return Json(new { success = false, message = "Error adding movie: " + ex.Message });
            }
        }

        // POST: Update Movie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] MovieFormData data)
        {
            try
            {
                var movie = await _context.Movies.FindAsync(data.Id);
                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found" });
                }

                movie.Title = data.Title;
                movie.Description = data.Description;
                movie.Genre = data.Genre;
                movie.ReleaseDate = data.ReleaseDate;
                movie.Duration = data.Duration;
                movie.Rating = data.Rating;
                movie.PosterUrl = data.PosterUrl;
                movie.BackdropUrl = data.BackdropUrl;
                movie.TrailerUrl = data.TrailerUrl;
                movie.Language = data.Language;
                movie.Country = data.Country;
                movie.ImdbId = data.ImdbId;
                movie.TmdbId = data.TmdbId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Movie updated: {Title} (ID: {Id})", movie.Title, movie.Id);
                return Json(new { success = true, message = "Movie updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie");
                return Json(new { success = false, message = "Error updating movie: " + ex.Message });
            }
        }

        // POST: Delete Movie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            try
            {
                var movie = await _context.Movies.FindAsync(id);
                if (movie == null)
                {
                    return Json(new { success = false, message = "Movie not found" });
                }

                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Movie deleted: {Title} (ID: {Id})", movie.Title, movie.Id);
                return Json(new { success = true, message = "Movie deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie");
                return Json(new { success = false, message = "Error deleting movie: " + ex.Message });
            }
        }

        // ✅ NEW: Helper method to get current user ID (same as AdvisorController)
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
    }

    // DTO for movie form data
    public class MovieFormData
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? Duration { get; set; }
        public decimal? Rating { get; set; }
        public string? PosterUrl { get; set; }
        public string? BackdropUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public string? Language { get; set; }
        public string? Country { get; set; }
        public string? ImdbId { get; set; }
        public int? TmdbId { get; set; }
    }
}