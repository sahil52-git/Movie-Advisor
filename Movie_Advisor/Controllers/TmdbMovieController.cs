using Microsoft.AspNetCore.Mvc;
using Movie_Advisor.Services;

namespace Movie_Advisor.Controllers
{
    public class TmdbMovieController : Controller
    {
        private readonly TmdbService _tmdbService;

        public TmdbMovieController(TmdbService tmdbService)
        {
            _tmdbService = tmdbService;
        }

        // GET: /TmdbMovie or /TmdbMovie/Index
        public async Task<IActionResult> Index(string searchQuery = "", int page = 1)
        {
            ViewBag.SearchQuery = searchQuery;
            ViewBag.CurrentPage = page;

            // If there's a search query, search for movies
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var searchResults = await _tmdbService.SearchMoviesAsync(searchQuery, page);
                ViewBag.IsSearchResult = true;
                return View(searchResults);
            }

            // Otherwise, show popular movies
            var popularMovies = await _tmdbService.GetPopularMoviesAsync(page);
            ViewBag.IsSearchResult = false;
            return View(popularMovies);
        }

        // GET: /TmdbMovie/NowPlaying
        public async Task<IActionResult> NowPlaying(int page = 1)
        {
            var movies = await _tmdbService.GetNowPlayingMoviesAsync(page);
            ViewBag.Title = "Now Playing";
            ViewBag.CurrentPage = page;
            return View("Index", movies);
        }

        // GET: /TmdbMovie/TopRated
        public async Task<IActionResult> TopRated(int page = 1)
        {
            var movies = await _tmdbService.GetTopRatedMoviesAsync(page);
            ViewBag.Title = "Top Rated";
            ViewBag.CurrentPage = page;
            return View("Index", movies);
        }

        // GET: /TmdbMovie/Details/123
        public async Task<IActionResult> Details(int id)
        {
            var movieDetails = await _tmdbService.GetMovieDetailsAsync(id);

            if (movieDetails == null)
            {
                TempData["Error"] = "Movie not found or unavailable.";
                return RedirectToAction("Index");
            }

            return View(movieDetails);
        }
    }
}