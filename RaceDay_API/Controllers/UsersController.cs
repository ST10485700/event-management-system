using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;
using RaceDay_API.Security;

namespace RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public UsersController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Registers a new Participant account. Public.</summary>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromForm] RegisterDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return Conflict(new { message = "An account with this email already exists." });
            }

            var newUser = new User
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Participant"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                newUser.UserID,
                newUser.FullName,
                newUser.Email,
                newUser.Role
            });
        }

        /// <summary>Logs in with email and password and starts a session. Public.</summary>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromForm] LoginDto dto)
        {
            var email = dto.Email.Trim().ToLower();
            var found = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (found == null || !BCrypt.Net.BCrypt.Verify(dto.Password, found.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserID", found.UserID);
            HttpContext.Session.SetString("Role", found.Role);

            return Ok(new
            {
                message = "Login successful.",
                found.UserID,
                found.FullName,
                found.Role
            });
        }

        /// <summary>Ends the current session. Any logged-in user.</summary>
        [HttpPost("logout")]
        [SessionAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out." });
        }

        /// <summary>Returns the profile of the logged-in user.</summary>
        [HttpGet("profile")]
        [SessionAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserID")!.Value;

            var profile = await _context.Users.AsNoTracking()
                .Where(u => u.UserID == userId)
                .Select(u => new { u.UserID, u.FullName, u.Email, u.Role, u.CreatedAt })
                .FirstOrDefaultAsync();

            if (profile == null)
                return NotFound(new { message = "User not found." });

            return Ok(profile);
        }

        /// <summary>Updates the name and email of the logged-in user.</summary>
        [HttpPut("profile")]
        [SessionAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserID")!.Value;
            var email = dto.Email.Trim().ToLower();

            var current = await _context.Users.FindAsync(userId);
            if (current == null)
                return NotFound(new { message = "User not found." });

            if (await _context.Users.AnyAsync(u => u.Email == email && u.UserID != userId))
                return Conflict(new { message = "That email is already used by another account." });

            current.FullName = dto.FullName.Trim();
            current.Email = email;
            await _context.SaveChangesAsync();

            return Ok(new { current.UserID, current.FullName, current.Email, current.Role });
        }
    }
}