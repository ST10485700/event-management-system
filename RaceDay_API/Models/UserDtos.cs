using System.ComponentModel.DataAnnotations;

namespace RaceDay_API.Models;

public class RegisterDto
{
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = string.Empty;
}