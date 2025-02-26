using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LinktrBackend.Data;
using LinktrBackend.Models;

namespace LinktrBackend.Controllers
{
    [ApiController]
    [Route("api")]
    public class ReferralController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReferralController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/referrals
        // Returns the list of referral records for the logged-in user.
        [HttpGet("referrals")]
        [Authorize]
        public async Task<IActionResult> GetReferrals()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var referrals = await _context.Referrals
                .Where(r => r.ReferrerId == userId)
                .ToListAsync();
            return Ok(referrals);
        }

        // GET: /api/referral-stats
        // Returns referral count statistics.
        [HttpGet("referral-stats")]
        [Authorize]
        public async Task<IActionResult> GetReferralStats()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var referralCount = await _context.Referrals
                .CountAsync(r => r.ReferrerId == userId && r.Status == "successful");
            return Ok(new { referralCount });
        }
    }
}
