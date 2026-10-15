using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;
using RaceDay_API.Security;

namespace RaceDay_API.Controllers
{
    public class EnrolDto
    {
        [Range(1, int.MaxValue)] public int CategoryID { get; set; }
    }

    public class EnrolmentView
    {
        public int EnrolmentID { get; set; }
        public string? BibNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public int ParticipantID { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal EntryFee { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserID")!.Value;
        private string CurrentRole => HttpContext.Session.GetString("Role") ?? "";

        private IActionResult Forbidden(string message) =>
            StatusCode(StatusCodes.Status403Forbidden, new { message });

        private static readonly Expression<Func<EventEnrolment, EnrolmentView>> ToView = e => new EnrolmentView
        {
            EnrolmentID = e.EnrolmentID,
            BibNumber = e.BibNumber,
            Status = e.Status,
            EnrolmentDate = e.EnrolmentDate,
            ParticipantID = e.ParticipantID,
            ParticipantName = e.Participant!.FullName,
            CategoryID = e.CategoryID,
            CategoryName = e.Category!.Name,
            EntryFee = e.Category!.EntryFee,
            EventID = e.Category!.EventID,
            EventName = e.Category!.Event!.Name,
            EventDate = e.Category!.Event!.EventDate
        };

        /// <summary>Enters the logged-in Participant into an event category. A bib number is assigned automatically.</summary>
        [HttpPost]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(EnrolmentView), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol([FromForm] EnrolDto dto)
        {
            var category = await _context.Categories.Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryID);
            if (category == null)
                return NotFound(new { message = $"Category with ID {dto.CategoryID} was not found." });

            if (category.Event!.EventDate < DateOnly.FromDateTime(DateTime.Today))
                return BadRequest(new { message = "This event has already taken place." });

            var participantId = CurrentUserId;

            if (await _context.EventEnrolments.AnyAsync(e =>
                    e.ParticipantID == participantId && e.CategoryID == dto.CategoryID))
                return Conflict(new { message = "You are already enrolled in this category." });

            var taken = await _context.EventEnrolments
                .CountAsync(e => e.CategoryID == dto.CategoryID && e.Status != "Cancelled");
            if (taken >= category.MaxParticipants)
                return Conflict(new { message = "This category is full." });

            var existingBibs = await _context.EventEnrolments
                .Where(e => e.BibNumber != null && e.BibNumber.StartsWith("RD"))
                .Select(e => e.BibNumber!).ToListAsync();
            var nextBib = existingBibs
                .Select(b => int.TryParse(b.Substring(2), out var n) ? n : 0)
                .DefaultIfEmpty(1000).Max() + 1;

            var enrolment = new EventEnrolment
            {
                ParticipantID = participantId,
                CategoryID = dto.CategoryID,
                Status = "Active",
                BibNumber = $"RD{nextBib}"
            };

            _context.EventEnrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            var view = await _context.EventEnrolments.AsNoTracking()
                .Where(e => e.EnrolmentID == enrolment.EnrolmentID).Select(ToView).FirstAsync();
            return CreatedAtAction(nameof(GetEnrolment), new { id = enrolment.EnrolmentID }, view);
        }

        /// <summary>Lists the logged-in Participant's own enrolments.</summary>
        [HttpGet("my")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(List<EnrolmentView>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var participantId = CurrentUserId;
            var list = await _context.EventEnrolments.AsNoTracking()
                .Where(e => e.ParticipantID == participantId)
                .OrderBy(e => e.Category!.Event!.EventDate)
                .Select(ToView).ToListAsync();
            return Ok(list);
        }

        /// <summary>Lists all enrolments for the Organiser's own events, optionally for one event.</summary>
        [HttpGet]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(List<EnrolmentView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEnrolments([FromQuery] int? eventId)
        {
            var organiserId = CurrentUserId;
            var query = _context.EventEnrolments.AsNoTracking()
                .Where(e => e.Category!.Event!.OrganiserID == organiserId);

            if (eventId.HasValue)
                query = query.Where(e => e.Category!.EventID == eventId.Value);

            var list = await query.OrderBy(e => e.Category!.EventID).ThenBy(e => e.BibNumber)
                .Select(ToView).ToListAsync();
            return Ok(list);
        }

        /// <summary>Returns one enrolment. Only its Participant or the event's Organiser.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EnrolmentView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            var enrolment = await _context.EventEnrolments.AsNoTracking()
                .Include(e => e.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);
            if (enrolment == null)
                return NotFound(new { message = $"Enrolment with ID {id} was not found." });

            var isOwner = CurrentRole == "Participant" && enrolment.ParticipantID == CurrentUserId;
            var isOrganiser = CurrentRole == "Organiser" &&
                              enrolment.Category!.Event!.OrganiserID == CurrentUserId;
            if (!isOwner && !isOrganiser)
                return Forbidden("You do not have access to this enrolment.");

            var view = await _context.EventEnrolments.AsNoTracking()
                .Where(e => e.EnrolmentID == id).Select(ToView).FirstAsync();
            return Ok(view);
        }

        /// <summary>Cancels the Participant's own enrolment.</summary>
        [HttpPut("{id:int}/cancel")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(EnrolmentView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var enrolment = await _context.EventEnrolments.FindAsync(id);
            if (enrolment == null)
                return NotFound(new { message = $"Enrolment with ID {id} was not found." });

            if (enrolment.ParticipantID != CurrentUserId)
                return Forbidden("You can only cancel your own enrolments.");

            if (enrolment.Status == "Cancelled")
                return BadRequest(new { message = "This enrolment is already cancelled." });

            if (await _context.Results.AnyAsync(r => r.EnrolmentID == id))
                return Conflict(new { message = "A result has already been captured for this enrolment." });

            enrolment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            var view = await _context.EventEnrolments.AsNoTracking()
                .Where(e => e.EnrolmentID == id).Select(ToView).FirstAsync();
            return Ok(view);
        }

        /// <summary>Removes an enrolment from the Organiser's own event (only if it has no result).</summary>
        [HttpDelete("{id:int}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEnrolment(int id)
        {
            var enrolment = await _context.EventEnrolments
                .Include(e => e.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);
            if (enrolment == null)
                return NotFound(new { message = $"Enrolment with ID {id} was not found." });

            if (enrolment.Category!.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only remove enrolments from your own events.");

            if (await _context.Results.AnyAsync(r => r.EnrolmentID == id))
                return Conflict(new { message = "Delete the result for this enrolment first." });

            _context.EventEnrolments.Remove(enrolment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}