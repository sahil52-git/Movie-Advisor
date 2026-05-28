namespace Movie_Advisor.ViewModels
{
    public class WatchHistoryViewModel
    {
        public List<WatchHistoryItemViewModel> WatchedMovies { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalMovies { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; } = "recent";
    }

    public class WatchHistoryItemViewModel
    {
        public int WatchedId { get; set; }
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Genre { get; set; }
        public int? Duration { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public decimal? Rating { get; set; }
        public string? PosterUrl { get; set; }
        public string? BackdropUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public DateTime WatchedAt { get; set; }
        public string WatchedAtFormatted { get; set; } = string.Empty;
    }
}