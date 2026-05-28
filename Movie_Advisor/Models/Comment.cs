using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movie_Advisor.Models
{
    [Table("COMMENTS")]
    public class Comment
    {
        [Key]
        [Column("COMMENT_ID")]  // ✅ Fixed: Changed from "ID" to "COMMENT_ID"
        public int Id { get; set; }

        [Required]
        [Column("MOVIE_ID")]
        public int MovieId { get; set; }

        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(2000)]
        [Column("COMMENT_TEXT")]
        public string CommentText { get; set; } = string.Empty;

        [Column("IS_APPROVED")]
        public bool IsApproved { get; set; } = false;

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("MovieId")]
        public virtual Movie? Movie { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}