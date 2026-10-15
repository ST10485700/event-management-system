using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;
using RaceDay_API.Security;

namespace RaceDay_API.Controllers
{
    public class CategoryUpdateDto
    {
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
        [Range(1, 100000)] public int MaxParticipants { get; set; }
        [Range(0, 100000)] public decimal EntryFee { get; set; }
    }

    public class CategoryCreateDto : CategoryUpdateDto
    {
        [Range(1, int.MaxValue)] public int EventID { get; set; }
    }

    public class CategoryView
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public int MaxParticipants { get; set; }
        public decimal EntryFee { get; set; }
        public int SpotsRemaining { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserID")!.Value;

        private IActionResult Forbidden(string message) =>
            StatusCode(StatusCodes.Status403Forbidden, new { message });

        private static readonly Expression<Func<Category, CategoryView>> ToView = c => new CategoryView
        {
            CategoryID = c.CategoryID,
            EventID = c.EventID,
            EventName = c.Event!.Name,
            Name = c.Name,
            DistanceKm = c.DistanceKm,
            MaxParticipants = c.MaxParticipants,
            EntryFee = c.EntryFee,
            SpotsRemaining = c.MaxParticipants - c.Enrolments.Count(x => x.Status != "Cancelled")
        };

        /// <summary>Lists categories, optionally filtered by event. Any logged-in user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryView>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories([FromQuery] int? eventId)
        {
            var query = _context.Categories.AsNoTracking().AsQueryable();
            if (eventId.HasValue)
                query = query.Where(c => c.EventID == eventId.Value);

            var list = await query.OrderBy(c => c.EventID).ThenBy(c => c.DistanceKm)
                .Select(ToView).ToListAsync();
            return Ok(list);
        }

        /// <summary>Returns one category by ID. Any logged-in user.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var item = await _context.Categories.AsNoTracking()
                .Where(c => c.CategoryID == id).Select(ToView).FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = $"Category with ID {id} was not found." });

            return Ok(item);
        }

        /// <summary>Creates a category for an event the Organiser owns.</summary>
        [HttpPost]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(CategoryView), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory([FromForm] CategoryCreateDto dto)
        {
            var ev = await _context.Events.FindAsync(dto.EventID);
            if (ev == null)
                return NotFound(new { message = $"Event with ID {dto.EventID} was not found." });

            if (ev.OrganiserID != CurrentUserId)
                return Forbidden("You can only add categories to your own events.");

            var name = dto.Name.Trim();
            if (await _context.Categories.AnyAsync(c => c.EventID == dto.EventID && c.Name == name))
                return Conflict(new { message = "This event already has a category with that name." });

            var category = new Category
            {
                EventID = dto.EventID,
                Name = name,
                DistanceKm = dto.DistanceKm,
                MaxParticipants = dto.MaxParticipants,
                EntryFee = dto.EntryFee
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var view = await _context.Categories.AsNoTracking()
                .Where(c => c.CategoryID == category.CategoryID).Select(ToView).FirstAsync();
            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryID }, view);
        }

        /// <summary>Updates a category. Only the Organiser who owns the event.</summary>
        [HttpPut("{id:int}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(CategoryView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] CategoryUpdateDto dto)
        {
            var category = await _context.Categories.Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null)
                return NotFound(new { message = $"Category with ID {id} was not found." });

            if (category.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only edit categories of your own events.");

            var name = dto.Name.Trim();
            if (await _context.Categories.AnyAsync(c =>
                    c.EventID == category.EventID && c.Name == name && c.CategoryID != id))
                return Conflict(new { message = "This event already has a category with that name." });

            var taken = await _context.EventEnrolments
                .CountAsync(e => e.CategoryID == id && e.Status != "Cancelled");
            if (dto.MaxParticipants < taken)
                return BadRequest(new { message = $"MaxParticipants cannot be below the {taken} current enrolments." });

            category.Name = name;
            category.DistanceKm = dto.DistanceKm;
            category.MaxParticipants = dto.MaxParticipants;
            category.EntryFee = dto.EntryFee;
            await _context.SaveChangesAsync();

            var view = await _context.Categories.AsNoTracking()
                .Where(c => c.CategoryID == id).Select(ToView).FirstAsync();
            return Ok(view);
        }

        /// <summary>Deletes a category that has no enrolments or routes.</summary>
        [HttpDelete("{id:int}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null)
                return NotFound(new { message = $"Category with ID {id} was not found." });

            if (category.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only delete categories of your own events.");

            if (await _context.EventEnrolments.AnyAsync(e => e.CategoryID == id) ||
                await _context.Routes.AnyAsync(r => r.CategoryID == id))
                return Conflict(new { message = "This category still has enrolments or routes." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}