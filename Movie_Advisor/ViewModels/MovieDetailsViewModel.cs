using Movie_Advisor.Models;
using MovieAdvisor.Models;

namespace Movie_Advisor.ViewModels
{
    public class MovieDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public string? Genre { get; set; }
        public int Duration { get; set; }
        public int ReleaseYear { get; set; }
        public string? Rating { get; set; }
        public decimal IMDBRating { get; set; }
        public decimal UserRating { get; set; }
        public string? TotalRatings { get; set; }
        public string? Description { get; set; }
        public string? PosterUrl { get; set; }
        public string? BackdropUrl { get; set; }
        public string? Director { get; set; }
        public string? Writer { get; set; }
        public string? Country { get; set; }
        public string? Language { get; set; }
        public string? Budget { get; set; }
        public string? BoxOffice { get; set; }

        // Trailer Support
        public string? TrailerKey { get; set; } // YouTube video ID
        public bool HasTrailer => !string.IsNullOrEmpty(TrailerKey);

        public bool UserHasRated { get; set; }
        public int UserRatingValue { get; set; }

        // ✅ ADD THESE TWO PROPERTIES
        public int CurrentUserId { get; set; }
        public bool IsAdmin { get; set; }

        // Helper method to get genres as list
        public List<string> GetGenres()
        {
            if (string.IsNullOrEmpty(Genre))
                return new List<string>();

            return Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(g => g.Trim())
                       .ToList();
        }

        // Helper method to format duration (e.g., "142 min" or "2h 22m")
        public string GetFormattedDuration()
        {
            if (Duration <= 0) return "N/A";

            var hours = Duration / 60;
            var minutes = Duration % 60;

            if (hours > 0 && minutes > 0)
                return $"{hours}h {minutes}m";
            else if (hours > 0)
                return $"{hours}h";
            else
                return $"{minutes}m";
        }
    }
}