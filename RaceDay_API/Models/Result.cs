// Models/Result.cs
using System.ComponentModel.DataAnnotations;

namespace RaceDay_API.Models;

public class Result
{
    [Key] public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public int CapturedByUserID { get; set; }
    public DateTime CapturedAt { get; set; }

    public EventEnrolment? Enrolment { get; set; }
    public User? CapturedBy { get; set; }
}