// Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace RaceDay_API.Models;

public class User
{
    [Key] public int UserID { get; set; }
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string PasswordHash { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Role { get; set; } = "Participant";
    public DateTime CreatedAt { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
}