using System.ComponentModel.DataAnnotations;

namespace LinktrBackend.Models
{
    public class Referral
    {
        [Key]
        public int Id { get; set; }

        public int ReferrerId { get; set; }
        public int ReferredUserId { get; set; }
        public DateTime DateReferred { get; set; } = DateTime.UtcNow;

        // E.g., "pending" or "successful"
        public string Status { get; set; } = "pending";
    }
}
