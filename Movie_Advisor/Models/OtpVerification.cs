using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movie_Advisor.Models
{
    [Table("OTP_VERIFICATIONS")]
    public class OtpVerification
    {
        [Key]
        [Column("OTP_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("EMAIL")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("OTP_CODE")]
        [StringLength(6)]
        public string OtpCode { get; set; } = string.Empty;

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("EXPIRES_AT")]
        public DateTime ExpiresAt { get; set; }

        [Column("ATTEMPTS")]
        public int Attempts { get; set; } = 0;

        [Column("IS_USED")]
        public bool IsUsed { get; set; } = false;

        [Column("VERIFIED_AT")]
        public DateTime? VerifiedAt { get; set; }

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        [NotMapped]
        public bool IsValid => !IsUsed && !IsExpired && Attempts < 3;
    }
}