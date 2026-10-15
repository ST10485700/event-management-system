using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;
using RaceDay_API.Security;

namespace RaceDay_API.Controllers
{
    public class ResultUpdateDto
    {
        [Required] public TimeSpan FinishTime { get; set; }
        [Range(1, 100000)] public int Position { get; set; }
    }

    public class ResultCreateDto : ResultUpdateDto
    {
        [Range(1, int.MaxValue)] public int EnrolmentID { get; set; }
    }

    public class ResultView
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public string? BibNumber { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
        public DateTime CapturedAt { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserID")!.Value;

        private IActionResult Forbidden(string message) =>
            StatusCode(StatusCodes.Status403Forbidden, new { message });

        private static readonly Expression<Func<Result, ResultView>> ToView = r => new ResultView
        {
            ResultID = r.ResultID,
            EnrolmentID = r.EnrolmentID,
            BibNumber = r.Enrolment!.BibNumber,
            ParticipantName = r.Enrolment!.Participant!.FullName,
            EventID = r.Enrolment!.Category!.EventID,
            EventName = r.Enrolment!.Category!.Event!.Name,
            EventDate = r.Enrolment!.Category!.Event!.EventDate,
            CategoryID = r.Enrolment!.CategoryID,
            CategoryName = r.Enrolment!.Category!.Name,
            FinishTime = r.FinishTime,
            Position = r.Position,
            CapturedAt = r.CapturedAt
        };

        /// <summary>Lists results for the Organiser's own events, optionally for one event.</summary>
        [HttpGet]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(List<ResultView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetResults([FromQuery] int? eventId)
        {
            var organiserId = CurrentUserId;
            var query = _context.Results.AsNoTracking()
                .Where(r => r.Enrolment!.Category!.Event!.OrganiserID == organiserId);

            if (eventId.HasValue)
                query = query.Where(r => r.Enrolment!.Category!.EventID == eventId.Value);

            var list = await query
                .OrderBy(r => r.Enrolment!.CategoryID).ThenBy(r => r.Position)
                .Select(ToView).ToListAsync();
            return Ok(list);
        }

        /// <summary>Lists the logged-in Participant's own results.</summary>
        [HttpGet("my")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(List<ResultView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults()
        {
            var participantId = CurrentUserId;
            var list = await _context.Results.AsNoTracking()
                .Where(r => r.Enrolment!.ParticipantID == participantId)
                .OrderByDescending(r => r.Enrolment!.Category!.Event!.EventDate)
                .Select(ToView).ToListAsync();
            return Ok(list);
        }

        /// <summary>Captures a finish time and position for an enrolment of the Organiser's own event.</summary>
        [HttpPost]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(ResultView), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CaptureResult([FromForm] ResultCreateDto dto)
        {
            if (dto.FinishTime <= TimeSpan.Zero)
                return BadRequest(new { message = "FinishTime must be greater than zero." });

            var enrolment = await _context.EventEnrolments
                .Include(e => e.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == dto.EnrolmentID);
            if (enrolment == null)
                return NotFound(new { message = $"Enrolment with ID {dto.EnrolmentID} was not found." });

            if (enrolment.Category!.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only capture results for your own events.");

            if (enrolment.Status == "Cancelled")
                return BadRequest(new { message = "A cancelled enrolment cannot have a result." });

            if (await _context.Results.AnyAsync(r => r.EnrolmentID == dto.EnrolmentID))
                return Conflict(new { message = "A result already exists for this enrolment." });

            if (await _context.Results.AnyAsync(r =>
                    r.Position == dto.Position && r.Enrolment!.CategoryID == enrolment.CategoryID))
                return Conflict(new { message = $"Position {dto.Position} is already taken in this category." });

            var result = new Result
            {
                EnrolmentID = dto.EnrolmentID,
                FinishTime = dto.FinishTime,
                Position = dto.Position,
                CapturedByUserID = CurrentUserId
            };

            enrolment.Status = "Completed";
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var view = await _context.Results.AsNoTracking()
                .Where(r => r.ResultID == result.ResultID).Select(ToView).FirstAsync();
            return CreatedAtAction(nameof(GetMyResults), new { }, view);
        }

        /// <summary>Corrects a captured result. Only the Organiser who owns the event.</summary>
        [HttpPut("{id:int}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(ResultView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateResult(int id, [FromForm] ResultUpdateDto dto)
        {
            if (dto.FinishTime <= TimeSpan.Zero)
                return BadRequest(new { message = "FinishTime must be greater than zero." });

            var result = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);
            if (result == null)
                return NotFound(new { message = $"Result with ID {id} was not found." });

            if (result.Enrolment!.Category!.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only edit results of your own events.");

            if (await _context.Results.AnyAsync(r =>
                    r.ResultID != id && r.Position == dto.Position &&
                    r.Enrolment!.CategoryID == result.Enrolment.CategoryID))
                return Conflict(new { message = $"Position {dto.Position} is already taken in this category." });

            result.FinishTime = dto.FinishTime;
            result.Position = dto.Position;
            await _context.SaveChangesAsync();

            var view = await _context.Results.AsNoTracking()
                .Where(r => r.ResultID == id).Select(ToView).FirstAsync();
            return Ok(view);
        }

        /// <summary>Deletes a result. Only the Organiser who owns the event.</summary>
        [HttpDelete("{id:int}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var result = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);
            if (result == null)
                return NotFound(new { message = $"Result with ID {id} was not found." });

            if (result.Enrolment!.Category!.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only delete results of your own events.");

            result.Enrolment.Status = "Active";
            _context.Results.Remove(result);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}