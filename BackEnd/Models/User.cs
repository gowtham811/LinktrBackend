using System.ComponentModel.DataAnnotations;
namespace LinktrBackend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; }

        public string ReferralCode { get; set; }

        // If the user was referred by someone, store that user's id
        public int? ReferredBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for referrals (optional)
        public List<Referral> Referrals { get; set; }
    }
}
