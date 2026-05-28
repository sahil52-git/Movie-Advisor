using System.Text.Json.Serialization;

namespace Movie_Advisor.Models
{
    // Extended movie details model
    public class TmdbMovieDetails : TmdbMovie
    {
        [JsonPropertyName("runtime")]
        public int? Runtime { get; set; }

        [JsonPropertyName("budget")]
        public long Budget { get; set; }

        [JsonPropertyName("revenue")]
        public long Revenue { get; set; }

        [JsonPropertyName("genres")]
        public List<Genres> Genres { get; set; } = new();

        [JsonPropertyName("production_companies")]
        public List<ProductionCompany> ProductionCompanies { get; set; } = new();

        [JsonPropertyName("tagline")]
        public string? Tagline { get; set; }

        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("credits")]
        public Credits? Credits { get; set; }

        [JsonPropertyName("videos")]
        public VideoResponse? Videos { get; set; }

        [JsonPropertyName("similar")]
        public TmdbMovieResponse? Similar { get; set; }

        // Helper method to format runtime
        public string GetFormattedRuntime()
        {
            if (!Runtime.HasValue || Runtime.Value == 0)
                return "N/A";

            var hours = Runtime.Value / 60;
            var minutes = Runtime.Value % 60;

            if (hours > 0)
                return $"{hours}h {minutes}m";

            return $"{minutes}m";
        }

        // Helper method to format budget/revenue
        public string FormatMoney(long amount)
        {
            if (amount == 0)
                return "N/A";

            return $"${amount:N0}";
        }
    }

    public class Genres
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class ProductionCompany
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("logo_path")]
        public string? LogoPath { get; set; }
    }

    public class Credits
    {
        [JsonPropertyName("cast")]
        public List<Cast> Cast { get; set; } = new();

        [JsonPropertyName("crew")]
        public List<Crew> Crew { get; set; } = new();
    }

    public class Cast
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("character")]
        public string Character { get; set; } = string.Empty;

        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        public string GetProfileUrl()
        {
            if (string.IsNullOrEmpty(ProfilePath))
                return "/images/no-profile.jpg";

            return $"https://image.tmdb.org/t/p/w185{ProfilePath}";
        }
    }

    public class Crew
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("job")]
        public string Job { get; set; } = string.Empty;

        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }
    }

    public class VideoResponse
    {
        [JsonPropertyName("results")]
        public List<Video> Results { get; set; } = new();
    }

    public class Video
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("site")]
        public string Site { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("official")]
        public bool Official { get; set; }

        public string GetYouTubeUrl()
        {
            return $"https://www.youtube.com/embed/{Key}";
        }
    }
}