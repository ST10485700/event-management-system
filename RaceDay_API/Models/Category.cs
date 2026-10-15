// Models/Category.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay_API.Models;

public class Category
{
    [Key] public int CategoryID { get; set; }
    public int EventID { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Column(TypeName = "decimal(6,2)")] public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal EntryFee { get; set; }

    public Event? Event { get; set; }
    public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
    public ICollection<Route> Routes { get; set; } = new List<Route>();
}