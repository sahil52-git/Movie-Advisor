using System.Text.Json;
using System.Text.Json.Serialization;

namespace Movie_Advisor.Services // CHANGED FROM TestProject.Services
{
    public class YouTubeApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://www.googleapis.com/youtube/v3";

        public YouTubeApiService(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
        }

        // Search for movie trailers on YouTube
        public async Task<List<YouTubeVideo>> SearchMovieTrailerAsync(string movieTitle, int maxResults = 5)
        {
            try
            {
                var searchQuery = Uri.EscapeDataString($"{movieTitle} official trailer");
                var url = $"{_baseUrl}/search?part=snippet&q={searchQuery}&type=video&maxResults={maxResults}&key={_apiKey}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var searchResponse = JsonSerializer.Deserialize<YouTubeSearchResponse>(json);

                    if (searchResponse?.Items != null)
                    {
                        return searchResponse.Items.Select(item => new YouTubeVideo
                        {
                            VideoId = item.Id?.VideoId ?? string.Empty,
                            Title = item.Snippet?.Title ?? string.Empty,
                            Description = item.Snippet?.Description ?? string.Empty,
                            ThumbnailUrl = item.Snippet?.Thumbnails?.High?.Url ?? string.Empty,
                            PublishedAt = item.Snippet?.PublishedAt ?? DateTime.MinValue
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching YouTube for '{movieTitle}': {ex.Message}");
            }

            return new List<YouTubeVideo>();
        }

        // Get video details (views, likes, duration, etc.)
        public async Task<YouTubeVideoDetails?> GetVideoDetailsAsync(string videoId)
        {
            try
            {
                var url = $"{_baseUrl}/videos?part=snippet,statistics,contentDetails&id={videoId}&key={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var videoResponse = JsonSerializer.Deserialize<YouTubeVideoResponse>(json);

                    if (videoResponse?.Items != null && videoResponse.Items.Count > 0)
                    {
                        var item = videoResponse.Items[0];
                        return new YouTubeVideoDetails
                        {
                            VideoId = item.Id ?? string.Empty,
                            Title = item.Snippet?.Title ?? string.Empty,
                            Description = item.Snippet?.Description ?? string.Empty,
                            ViewCount = long.Parse(item.Statistics?.ViewCount ?? "0"),
                            LikeCount = long.Parse(item.Statistics?.LikeCount ?? "0"),
                            Duration = item.ContentDetails?.Duration ?? string.Empty
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting video details for {videoId}: {ex.Message}");
            }

            return null;
        }
    }

    // YouTube API Response Models
    public class YouTubeSearchResponse
    {
        [JsonPropertyName("items")]
        public List<YouTubeSearchItem> Items { get; set; } = new();
    }

    public class YouTubeSearchItem
    {
        [JsonPropertyName("id")]
        public YouTubeVideoId? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubeSnippet? Snippet { get; set; }
    }

    public class YouTubeVideoId
    {
        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }
    }

    public class YouTubeSnippet
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("thumbnails")]
        public YouTubeThumbnails? Thumbnails { get; set; }
    }

    public class YouTubeThumbnails
    {
        [JsonPropertyName("high")]
        public YouTubeThumbnail? High { get; set; }
    }

    public class YouTubeThumbnail
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class YouTubeVideoResponse
    {
        [JsonPropertyName("items")]
        public List<YouTubeVideoItem> Items { get; set; } = new();
    }

    public class YouTubeVideoItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubeSnippet? Snippet { get; set; }

        [JsonPropertyName("statistics")]
        public YouTubeStatistics? Statistics { get; set; }

        [JsonPropertyName("contentDetails")]
        public YouTubeContentDetails? ContentDetails { get; set; }
    }

    public class YouTubeStatistics
    {
        [JsonPropertyName("viewCount")]
        public string? ViewCount { get; set; }

        [JsonPropertyName("likeCount")]
        public string? LikeCount { get; set; }
    }

    public class YouTubeContentDetails
    {
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }
    }

    // Simplified models for database
    public class YouTubeVideo
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
    }

    public class YouTubeVideoDetails
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long ViewCount { get; set; }
        public long LikeCount { get; set; }
        public string Duration { get; set; } = string.Empty;
    }
}