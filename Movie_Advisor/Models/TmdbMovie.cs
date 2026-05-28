using System.Text.Json.Serialization;

namespace Movie_Advisor.Models
{
    public class TmdbMovie
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string Overview { get; set; } = string.Empty;

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("vote_count")]
        public int VoteCount { get; set; }

        [JsonPropertyName("popularity")]
        public double Popularity { get; set; }

        // Helper property to get full poster URL
        public string GetPosterUrl()
        {
            if (string.IsNullOrEmpty(PosterPath))
                return "/images/no-poster.jpg"; // Fallback image

            return $"https://image.tmdb.org/t/p/w500{PosterPath}";
        }

        // Helper property to get full backdrop URL
        public string GetBackdropUrl()
        {
            if (string.IsNullOrEmpty(BackdropPath))
                return "/images/no-backdrop.jpg"; // Fallback image

            return $"https://image.tmdb.org/t/p/w1280{BackdropPath}";
        }
    }

    // Response wrapper for TMDb API
    public class TmdbMovieResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("results")]
        public List<TmdbMovie> Results { get; set; } = new();

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }
    }
}