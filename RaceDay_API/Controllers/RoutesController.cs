using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;
using RaceDay_API.Security;
using RouteEntity = RaceDay_API.Models.Route;

namespace RaceDay_API.Controllers
{
    public class RouteUpdateDto
    {
        [Required, MaxLength(150)] public string RouteName { get; set; } = string.Empty;
        [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
        [Range(0, 100000)] public int ElevationGain { get; set; }
        [MaxLength(300), Url] public string? MapUrl { get; set; }
    }

    public class RouteCreateDto : RouteUpdateDto
    {
        [Range(1, int.MaxValue)] public int CategoryID { get; set; }
    }

    public class RouteView
    {
        public int RouteID { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public int ElevationGain { get; set; }
        public string? MapUrl { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class RoutesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public RoutesController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserID")!.Value;

        private IActionResult Forbidden(string message) =>
            StatusCode(StatusCodes.Status403Forbidden, new { message });

        private static readonly Expression<Func<RouteEntity, RouteView>> ToView = r => new RouteView
        {
            RouteID = r.RouteID,
            CategoryID = r.CategoryID,
            CategoryName = r.Category!.Name,
            EventName = r.Category!.Event!.Name,
            RouteName = r.RouteName,
            DistanceKm = r.DistanceKm,
            ElevationGain = r.ElevationGain,
            MapUrl = r.MapUrl
        };

        /// <summary>Lists routes, optionally filtered by category. Any logged-in user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<RouteView>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoutes([FromQuery] int? categoryId)
        {
            var query = _context.Routes.AsNoTracking().AsQueryable();
            if (categoryId.HasValue)
                query = query.Where(r => r.CategoryID == categoryId.Value);

            return Ok(await query.OrderBy(r => r.CategoryID).Select(ToView).ToListAsync());
        }

        /// <summary>Returns one route by ID. Any logged-in user.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(RouteView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoute(int id)
        {
            var item = await _context.Routes.AsNoTracking()
                .Where(r => r.RouteID == id).Select(ToView).FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = $"Route with ID {id} was not found." });

            return Ok(item);
        }

        /// <summary>Creates a route for a category of an event the Organiser owns.</summary>
        [HttpPost]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(RouteView), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateRoute([FromForm] RouteCreateDto dto)
        {
            var category = await _context.Categories.Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryID);
            if (category == null)
                return NotFound(new { message = $"Category with ID {dto.CategoryID} was not found." });

            if (category.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only add routes to your own events.");

            var route = new RouteEntity
            {
                CategoryID = dto.CategoryID,
                RouteName = dto.RouteName.Trim(),
                DistanceKm = dto.DistanceKm,
                ElevationGain = dto.ElevationGain,
                MapUrl = dto.MapUrl?.Trim()
            };

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            var view = await _context.Routes.AsNoTracking()
                .Where(r => r.RouteID == route.RouteID).Select(ToView).FirstAsync();
            return CreatedAtAction(nameof(GetRoute), new { id = route.RouteID }, view);
        }

        /// <summary>Updates a route. Only the Organiser who owns the event.</summary>
        [HttpPut("{id:int}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(RouteView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRoute(int id, [FromForm] RouteUpdateDto dto)
        {
            var route = await _context.Routes.Include(r => r.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.RouteID == id);
            if (route == null)
                return NotFound(new { message = $"Route with ID {id} was not found." });

            if (route.Category!.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only edit routes of your own events.");

            route.RouteName = dto.RouteName.Trim();
            route.DistanceKm = dto.DistanceKm;
            route.ElevationGain = dto.ElevationGain;
            route.MapUrl = dto.MapUrl?.Trim();
            await _context.SaveChangesAsync();

            var view = await _context.Routes.AsNoTracking()
                .Where(r => r.RouteID == id).Select(ToView).FirstAsync();
            return Ok(view);
        }

        /// <summary>Deletes a route. Only the Organiser who owns the event.</summary>
        [HttpDelete("{id:int}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _context.Routes.Include(r => r.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.RouteID == id);
            if (route == null)
                return NotFound(new { message = $"Route with ID {id} was not found." });

            if (route.Category!.Event!.OrganiserID != CurrentUserId)
                return Forbidden("You can only delete routes of your own events.");

            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}