using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movie_Advisor.Models
{
    [Table("WATCHED_MOVIES")]
    public class WatchedMovie
    {
        [Key]
        [Column("WATCHED_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [Column("MOVIE_ID")]
        public int MovieId { get; set; }

        [Column("WATCHED_AT")]
        public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("MovieId")]
        public virtual Movie? Movie { get; set; }
    }
}