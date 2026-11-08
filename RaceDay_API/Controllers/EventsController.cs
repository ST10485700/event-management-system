using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;
using RaceDay_API.Security;
using RaceDay_API.Services;

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

        /// <summary>Headline distance of the event in kilometres (optional).</summary>
        [Range(0.01, 1000)] public decimal? DistanceKm { get; set; }

        /// <summary>Optional banner image (JPG, PNG, WEBP or GIF, up to 5 MB), stored in Azure Blob Storage.</summary>
        public IFormFile? Banner { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IImageStorage _storage;

        public EventsController(RaceDayContext context, IImageStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserID")!.Value;

        // Validates and uploads the banner. Returns the URL (or null if no file) or an error response.
        private async Task<(string? Url, IActionResult? Error)> SaveBannerAsync(IFormFile? banner)
        {
            if (banner == null || banner.Length == 0)
            {
                return (null, null);
            }

            var problem = ImageValidation.Validate(banner);
            if (problem != null)
            {
                return (null, BadRequest(new { message = problem }));
            }

            try
            {
                var url = await _storage.UploadAsync(banner, "banners");
                return (url, null);
            }
            catch (ImageStorageException ex)
            {
                return (null, StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }));
            }
        }

        /// <summary>Lists events, soonest first, with optional filters. Any logged-in user.</summary>
        [HttpGet]
        [SessionAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string? search,
            [FromQuery] string? eventType,
            [FromQuery] string? location,
            [FromQuery(Name = "from")] DateOnly? fromDate,
            [FromQuery(Name = "to")] DateOnly? toDate)
        {
            var query = _context.Events.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(e => e.Name.Contains(term)
                                         || e.Location.Contains(term)
                                         || (e.Description != null && e.Description.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                var type = eventType.Trim();
                query = query.Where(e => e.EventType == type);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var place = location.Trim();
                query = query.Where(e => e.Location.Contains(place));
            }

            if (fromDate.HasValue)
            {
                var start = fromDate.Value;
                query = query.Where(e => e.EventDate >= start);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value;
                query = query.Where(e => e.EventDate <= end);
            }

            var events = await query
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.EventType,
                    e.DistanceKm,
                    e.BannerUrl,
                    e.OrganiserID,
                    OrganiserName = e.Organiser!.FullName
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>Lists the logged-in Organiser's own events with category and enrolment counts.</summary>
        [HttpGet("mine")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEvents()
        {
            var organiserId = CurrentUserId;

            var events = await _context.Events.AsNoTracking()
                .Where(e => e.OrganiserID == organiserId)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.EventType,
                    e.DistanceKm,
                    e.BannerUrl,
                    CategoryCount = e.Categories.Count(),
                    EnrolmentCount = e.Categories
                        .SelectMany(c => c.Enrolments)
                        .Count(en => en.Status != "Cancelled")
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>Returns one event by ID. Any logged-in user.</summary>
        [HttpGet("{id:int}")]
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
                    e.DistanceKm,
                    e.BannerUrl,
                    e.OrganiserID,
                    OrganiserName = e.Organiser!.FullName
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = $"Event with ID {id} was not found." });

            return Ok(item);
        }

        /// <summary>Creates an event owned by the logged-in Organiser, with an optional banner image.</summary>
        [HttpPost]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CreateEvent([FromForm] EventDto dto)
        {
            if (dto.EventDate < DateOnly.FromDateTime(DateTime.Today))
                return BadRequest(new { message = "The event date cannot be in the past." });

            var (bannerUrl, error) = await SaveBannerAsync(dto.Banner);
            if (error != null)
                return error;

            var ev = new Event
            {
                OrganiserID = CurrentUserId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                EventDate = dto.EventDate,
                Location = dto.Location.Trim(),
                EventType = dto.EventType,
                DistanceKm = dto.DistanceKm,
                BannerUrl = bannerUrl
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
                ev.DistanceKm,
                ev.BannerUrl,
                ev.OrganiserID
            });
        }

        /// <summary>Updates an event. Only the Organiser who owns it. A new banner replaces the old one; no file keeps it.</summary>
        [HttpPut("{id:int}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UpdateEvent(int id, [FromForm] EventDto dto)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
                return NotFound(new { message = $"Event with ID {id} was not found." });

            if (ev.OrganiserID != CurrentUserId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "You can only edit your own events." });

            var (bannerUrl, error) = await SaveBannerAsync(dto.Banner);
            if (error != null)
                return error;

            ev.Name = dto.Name.Trim();
            ev.Description = dto.Description?.Trim();
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location.Trim();
            ev.EventType = dto.EventType;
            ev.DistanceKm = dto.DistanceKm;
            if (bannerUrl != null)
            {
                ev.BannerUrl = bannerUrl;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ev.EventID,
                ev.Name,
                ev.Description,
                ev.EventDate,
                ev.Location,
                ev.EventType,
                ev.DistanceKm,
                ev.BannerUrl,
                ev.OrganiserID
            });
        }

        /// <summary>Deletes an event. Only its Organiser, and only if it has no categories.</summary>
        [HttpDelete("{id:int}")]
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

            if (ev.OrganiserID != CurrentUserId)
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
