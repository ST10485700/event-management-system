using System.ComponentModel.DataAnnotations;

namespace RaceDay_Web.Models;

// ----- API shape for GET api/Events/mine -----

public class MyEventInfo
{
    public int EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
}

// ----- View models -----

public class DashboardViewModel
{
    public List<MyEventInfo> Events { get; set; } = new();
    public string? Error { get; set; }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e => e.EventDate >= Today);
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int TotalCategories => Events.Sum(e => e.CategoryCount);

    public MyEventInfo? NextEvent =>
        Events.Where(e => e.EventDate >= Today).OrderBy(e => e.EventDate).FirstOrDefault();
}

public class OrganiserEventsViewModel
{
    public List<MyEventInfo> Events { get; set; } = new();
    public string? Error { get; set; }
}

public class EventFormModel
{
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "The description can be up to 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [Display(Name = "Event date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event type.")]
    [RegularExpression("^(Running|Cycling|Walking)$", ErrorMessage = "Please choose Running, Cycling or Walking.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Running";

    [Required(ErrorMessage = "Please enter the distance in kilometres.")]
    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }
}

public class CategoryFormModel
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string? EventName { get; set; }

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in kilometres.")]
    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please enter the maximum number of participants.")]
    [Range(1, 100000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; }

    [Required(ErrorMessage = "Please enter the entry fee (0 for free).")]
    [Range(0, 100000, ErrorMessage = "The entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }
}

public class CategoriesViewModel
{
    public EventSummary Event { get; set; } = new();
    public List<CategoryInfo> Categories { get; set; } = new();
    public CategoryFormModel NewCategory { get; set; } = new();
    public string? Error { get; set; }
}

public class EventEnrolmentsViewModel
{
    public List<MyEventInfo> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public List<EnrolmentInfo> Enrolments { get; set; } = new();
    public string? Error { get; set; }

    public MyEventInfo? SelectedEvent => Events.FirstOrDefault(e => e.EventID == SelectedEventId);
}

public class ResultRow
{
    public EnrolmentInfo Enrolment { get; set; } = new();
    public ResultInfo? Result { get; set; }
}

public class ResultsEntryViewModel
{
    public List<MyEventInfo> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public List<ResultRow> Rows { get; set; } = new();
    public string? Error { get; set; }

    public MyEventInfo? SelectedEvent => Events.FirstOrDefault(e => e.EventID == SelectedEventId);
}
