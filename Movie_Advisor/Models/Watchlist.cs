using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movie_Advisor.Models
{
    [Table("WATCHLISTS")]
    public class Watchlist
    {
        [Key]
        [Column("WATCHLIST_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [Column("MOVIE_ID")]
        public int MovieId { get; set; }

        [Column("ADDED_AT")]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("MovieId")]
        public virtual Movie? Movie { get; set; }
    }
}