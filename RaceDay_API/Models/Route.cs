// Models/Route.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay_API.Models;

public class Route
{
    [Key] public int RouteID { get; set; }
    public int CategoryID { get; set; }
    [Required, MaxLength(150)] public string RouteName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(6,2)")] public decimal DistanceKm { get; set; }
    public int ElevationGain { get; set; }
    [MaxLength(300)] public string? MapUrl { get; set; }

    public Category? Category { get; set; }
}