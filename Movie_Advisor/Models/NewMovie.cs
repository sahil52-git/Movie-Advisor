namespace Movie_Advisor.Models
{
    public class NewMovie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Director { get; set; }
        public int ReleaseYear { get; set; }
        public string Genre { get; set; }
        public int Duration { get; set; }
        public string Description { get; set; }
        public double Rating { get; set; }
        public string PosterUrl { get; set; }
    }
}
