using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinktrBackend.Data;
using LinktrBackend.Models;
using LinktrBackend.Services;
using BCrypt.Net;

namespace LinktrBackend.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthController(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // POST: /api/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate required fields
            if (string.IsNullOrEmpty(request.Email) ||
               string.IsNullOrEmpty(request.Username) ||
               string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Missing required fields.");
            }

            // Check if email or username already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username))
            {
                return BadRequest("Email or username already in use.");
            }

            // Hash the password using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Generate a simple referral code
            string referralCode = Guid.NewGuid().ToString().Substring(0, 8);

            var newUser = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = passwordHash,
                ReferralCode = referralCode,
                CreatedAt = DateTime.UtcNow,
                ReferredBy = null
            };

            // Process referral code if provided
            if (!string.IsNullOrEmpty(request.ReferralCode))
            {
                var referrer = await _context.Users.FirstOrDefaultAsync(u => u.ReferralCode == request.ReferralCode);
                if (referrer == null)
                {
                    return BadRequest("Invalid referral code.");
                }
                newUser.ReferredBy = referrer.Id;

                // Create referral record (the new user's Id will be set after SaveChanges)
                var referral = new Referral
                {
                    ReferrerId = referrer.Id,
                    // For now, set ReferredUserId to 0; update it after saving newUser.
                    ReferredUserId = 0,
                    DateReferred = DateTime.UtcNow,
                    Status = "successful"
                };
                _context.Referrals.Add(referral);
            }

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // If referral was used, update the referral record with the new user's Id
            if (newUser.ReferredBy.HasValue)
            {
                var referral = await _context.Referrals
                    .FirstOrDefaultAsync(r => r.ReferredUserId == 0 && r.ReferrerId == newUser.ReferredBy.Value);
                if (referral != null)
                {
                    referral.ReferredUserId = newUser.Id;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "User registered successfully." });
        }

        // POST: /api/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.EmailOrUsername) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Missing credentials.");
            }

            // Find user by email or username
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);
            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            // Generate and return JWT token
            var token = _jwtService.GenerateToken(user.Id, user.Username);
            return Ok(new { token });
        }

        // POST: /api/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User with this email does not exist.");
            }

            // In production, generate a secure token and send an email.
            // For demo, we simply return a dummy token.
            var resetToken = Guid.NewGuid().ToString();

            return Ok(new { message = "Password reset link has been sent to your email.", resetToken });
        }
    }

    // DTO classes for request payloads
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ReferralCode { get; set; }
    }

    public class LoginRequest
    {
        public string EmailOrUsername { get; set; }
        public string Password { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }
}
