using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;
using RaceDay_API.Security;

namespace RaceDay_API.Controllers
{
    public class EventDto
    {
        [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
        [MaxLength(500)] public string? Description { get; set; }
        [Required] public DateOnly EventDate { get; set; }
        [Required, MaxLength(150)] public string Location { get; set; } = string.Empty;
        [Required, RegularExpression("^(Running|Cycling|Walking)$",
            ErrorMessage = "EventType must be Running, Cycling or Walking.")]
        public string EventType { get; set; } = string.Empty;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Lists all events, soonest first. Any logged-in user.</summary>
        [HttpGet]
        [SessionAuthorize]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events.AsNoTracking()
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.EventType,
                    e.OrganiserID,
                    OrganiserName = e.Organiser!.FullName
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>Returns one event by ID. Any logged-in user.</summary>
        [HttpGet("{id}")]
        [SessionAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var item = await _context.Events.AsNoTracking()
                .Where(e => e.EventID == id)
                .Select(e => new
                {
                    e.EventID,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.EventType,
                    e.OrganiserID,
                    OrganiserName = e.Organiser!.FullName
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = $"Event with ID {id} was not found." });

            return Ok(item);
        }

        /// <summary>Creates an event owned by the logged-in Organiser.</summary>
        [HttpPost]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent([FromForm] EventDto dto)
        {
            if (dto.EventDate < DateOnly.FromDateTime(DateTime.Today))
                return BadRequest(new { message = "The event date cannot be in the past." });

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

            var ev = new Event
            {
                OrganiserID = organiserId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                EventDate = dto.EventDate,
                Location = dto.Location.Trim(),
                EventType = dto.EventType
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, new
            {
                ev.EventID,
                ev.Name,
                ev.Description,
                ev.EventDate,
                ev.Location,
                ev.EventType,
                ev.OrganiserID
            });
        }

        /// <summary>Updates an event. Only the Organiser who owns it.</summary>
        [HttpPut("{id}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, [FromForm] EventDto dto)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
                return NotFound(new { message = $"Event with ID {id} was not found." });

            if (ev.OrganiserID != HttpContext.Session.GetInt32("UserID"))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "You can only edit your own events." });

            ev.Name = dto.Name.Trim();
            ev.Description = dto.Description?.Trim();
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location.Trim();
            ev.EventType = dto.EventType;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                ev.EventID,
                ev.Name,
                ev.Description,
                ev.EventDate,
                ev.Location,
                ev.EventType,
                ev.OrganiserID
            });
        }

        /// <summary>Deletes an event. Only its Organiser, and only if it has no categories.</summary>
        [HttpDelete("{id}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
                return NotFound(new { message = $"Event with ID {id} was not found." });

            if (ev.OrganiserID != HttpContext.Session.GetInt32("UserID"))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "You can only delete your own events." });

            if (await _context.Categories.AnyAsync(c => c.EventID == id))
                return Conflict(new { message = "Delete this event's categories first." });

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}