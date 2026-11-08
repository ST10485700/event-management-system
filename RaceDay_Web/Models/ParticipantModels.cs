using System.ComponentModel.DataAnnotations;

namespace RaceDay_Web.Models;

// ----- Shapes of the JSON the API sends back -----

public class EventSummary
{
    public int EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
    public int OrganiserID { get; set; }
    public string? OrganiserName { get; set; }
}

public class CategoryInfo
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

public class EnrolmentInfo
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

public class ResultInfo
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
    public int TotalFinishers { get; set; }
}

public class ProfileInfo
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

// ----- View models -----

public class EventsIndexViewModel
{
    public List<EventSummary> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public string? Location { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public bool IncludePast { get; set; }
    public string? Error { get; set; }
}

public class EventDetailsViewModel
{
    public EventSummary Event { get; set; } = new();
    public List<CategoryInfo> Categories { get; set; } = new();

    // categoryId -> the participant's enrolment status for it
    public Dictionary<int, string> MyEnrolmentStatus { get; set; } = new();

    public string? Error { get; set; }

    public bool IsPast => Event.EventDate < DateOnly.FromDateTime(DateTime.Today);
}

public class EnrolmentsViewModel
{
    public List<EnrolmentInfo> Enrolments { get; set; } = new();
    public string? Error { get; set; }
}

public class ResultsViewModel
{
    public List<ResultInfo> Results { get; set; } = new();
    public string? Error { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Profile picture")]
    public IFormFile? ProfilePicture { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public string? Role { get; set; }
}
