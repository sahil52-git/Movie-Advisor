namespace Movie_Advisor.Models
{
    public class UserGenrePreference
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GenreName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual User? User { get; set; }
    }
}