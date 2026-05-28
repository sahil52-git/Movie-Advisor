using Movie_Advisor.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movie_Advisor.Models
{
    [Table("MOVIES")]
    public class Movie
    {
        [Key]
        [Column("MOVIE_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("TITLE")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("PLOT")]
        [StringLength(2000)]
        public string? Description { get; set; }

        [Column("LANGUAGE")]
        [StringLength(50)]
        public string? Language { get; set; }

        [Column("COUNTRY")]
        [StringLength(100)]
        public string? Country { get; set; }

        [Column("RELEASE_DATE")]
        public DateTime? ReleaseDate { get; set; }

        [Column("DURATION")]
        public int? Duration { get; set; }

        [Column("RATING", TypeName = "NUMBER(3,1)")]
        [Range(0, 10)]
        public decimal? Rating { get; set; }

        [Column("POSTER_URL")]
        [StringLength(500)]
        public string? PosterUrl { get; set; }

        [Column("BACKDROP_URL")]
        [StringLength(500)]
        public string? BackdropUrl { get; set; }

        [Column("TRAILER_URL")]
        [StringLength(500)]
        public string? TrailerUrl { get; set; }

        [Column("IMDB_ID")]
        [StringLength(50)]
        public string? ImdbId { get; set; }

        [Column("TMDB_ID")]
        public int? TmdbId { get; set; }

        [Column("BUDGET", TypeName = "NUMBER(15,2)")]
        public decimal? Budget { get; set; }

        [Column("REVENUE", TypeName = "NUMBER(15,2)")]
        public decimal? Revenue { get; set; }

        // ✅ ADD THIS: Genre as comma-separated string
        [Column("GENRE")]
        [StringLength(500)]
        public string? Genre { get; set; }

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        
        // Computed properties for backward compatibility
        [NotMapped]
        public int? ReleaseYear => ReleaseDate?.Year;

        [NotMapped]
        public decimal? AverageRating => Rating;

        [NotMapped]
        public int? DurationMinutes => Duration;

        [NotMapped]
        public int? Year => ReleaseDate?.Year;

        [NotMapped]
        public DateTime DateAdded => CreatedAt;

        // ✅ ADD THIS: Helper property to get genres as list
        [NotMapped]
        public List<string> GenreList
        {
            get
            {
                if (string.IsNullOrEmpty(Genre))
                    return new List<string>();

                return Genre.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(g => g.Trim())
                           .ToList();
            }
        }

        // ✅ ADD THIS: Helper method to check if movie has a specific genre
        // NOTE: Methods don't need [NotMapped] attribute
        public bool HasGenre(string genreName)
        {
            if (string.IsNullOrEmpty(Genre) || string.IsNullOrEmpty(genreName))
                return false;

            return GenreList.Any(g =>
                g.Equals(genreName, StringComparison.OrdinalIgnoreCase));

        }
    }
}