using Movie_Advisor.Models;
using MovieAdvisor.Models;
using System.Text.Json;

namespace Movie_Advisor.Services
{
    public class TmdbService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.themoviedb.org/3";

        public TmdbService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["APIKeys:TMDB"]
                ?? throw new ArgumentNullException("TMDb API Key is missing in appsettings.json");
        }

        // Get Popular Movies
        public async Task<List<TmdbMovie>> GetPopularMoviesAsync(int page = 1)
        {
            try
            {
                var url = $"{_baseUrl}/movie/popular?api_key={_apiKey}&language=en-US&page={page}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var movieResponse = JsonSerializer.Deserialize<TmdbMovieResponse>(json);
                    return movieResponse?.Results ?? new List<TmdbMovie>();
                }

                return new List<TmdbMovie>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching popular movies: {ex.Message}");
                return new List<TmdbMovie>();
            }
        }

        // Search Movies
        public async Task<List<TmdbMovie>> SearchMoviesAsync(string query, int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new List<TmdbMovie>();

                var url = $"{_baseUrl}/search/movie?api_key={_apiKey}&language=en-US&query={Uri.EscapeDataString(query)}&page={page}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var movieResponse = JsonSerializer.Deserialize<TmdbMovieResponse>(json);
                    return movieResponse?.Results ?? new List<TmdbMovie>();
                }

                return new List<TmdbMovie>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching movies: {ex.Message}");
                return new List<TmdbMovie>();
            }
        }

        // Get Movie Details with Credits, Videos, and Similar Movies
        public async Task<TmdbMovieDetails?> GetMovieDetailsAsync(int movieId)
        {
            try
            {
                // Append credits, videos, and similar movies to the request
                var url = $"{_baseUrl}/movie/{movieId}?api_key={_apiKey}&language=en-US&append_to_response=credits,videos,similar";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TmdbMovieDetails>(json);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching movie details: {ex.Message}");
                return null;
            }
        }

        // Get Now Playing Movies
        public async Task<List<TmdbMovie>> GetNowPlayingMoviesAsync(int page = 1)
        {
            try
            {
                var url = $"{_baseUrl}/movie/now_playing?api_key={_apiKey}&language=en-US&page={page}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var movieResponse = JsonSerializer.Deserialize<TmdbMovieResponse>(json);
                    return movieResponse?.Results ?? new List<TmdbMovie>();
                }

                return new List<TmdbMovie>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching now playing movies: {ex.Message}");
                return new List<TmdbMovie>();
            }
        }

        // Get Top Rated Movies
        public async Task<List<TmdbMovie>> GetTopRatedMoviesAsync(int page = 1)
        {
            try
            {
                var url = $"{_baseUrl}/movie/top_rated?api_key={_apiKey}&language=en-US&page={page}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var movieResponse = JsonSerializer.Deserialize<TmdbMovieResponse>(json);
                    return movieResponse?.Results ?? new List<TmdbMovie>();
                }

                return new List<TmdbMovie>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching top rated movies: {ex.Message}");
                return new List<TmdbMovie>();
            }
        }
    }
}