// Models/Event.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay_API.Models;

public class Event
{
    [Key] public int EventID { get; set; }
    public int OrganiserID { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string EventType { get; set; } = string.Empty;

    // Added in Part 3
    [Column(TypeName = "decimal(6,2)")] public decimal? DistanceKm { get; set; }
    [MaxLength(500)] public string? BannerUrl { get; set; }

    public User? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
