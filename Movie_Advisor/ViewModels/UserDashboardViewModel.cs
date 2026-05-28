using Movie_Advisor.Models;

namespace Movie_Advisor.ViewModels
{
    public class UserDashboardViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime LastLoginDate { get; set; }
        public int TotalMoviesWatched { get; set; }
        public int TotalRecommendations { get; set; }
        public string? Email { get; set; }
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
        public List<MovieRecommendation> TopRecommendations { get; set; } = new List<MovieRecommendation>();
        public List<Movie> Movie { get; set; } = new List<Movie>();
        public int TotalMovies { get; set; }

        // Renamed from Genre to ViewGenre to avoid conflict
        public List<ViewGenre> Genres { get; set; } = new List<ViewGenre>();

        public List<Movie> TopIMDBMovies { get; set; } = new List<Movie>();
        public List<Movie> TVSeries { get; set; } = new List<Movie>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public string? SelectedGenre { get; set; }
        public int? SelectedYear { get; set; }
        public string? SelectedQuality { get; set; }
        public string? SortBy { get; set; }
        public string? SearchQuery { get; set; }
    }

    public class RecentActivity
    {
        public string Activity { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class MovieRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string PosterUrl { get; set; } = string.Empty;
    }

    // Renamed from Genre to ViewGenre to avoid conflicts
    public class ViewGenre
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int MovieCount { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class TvSeries
    {
        public string? Name { get; set; }
        public int Id { get; set; }
        public string? Description { get; set; }
    }

    public class DashboardMovieSummary
    {
        public string? Name { get; set; }
        public int Id { get; set; }
        public string? Description { get; set; }
    }
}