// Models/EventEnrolment.cs
using System.ComponentModel.DataAnnotations;

namespace RaceDay_API.Models;

public class EventEnrolment
{
    [Key] public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Active";
    [MaxLength(20)] public string? BibNumber { get; set; }

    public User? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}