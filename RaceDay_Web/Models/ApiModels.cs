namespace RaceDay_Web.Models;

// Shapes of the JSON the RaceDay API sends back.
// Property names are matched case-insensitively (the API sends camelCase).

public class LoginResponse
{
    public string? Message { get; set; }
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// Wraps every API call so controllers never deal with exceptions or raw errors.
public class ApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public string Error { get; init; } = string.Empty;

    public bool IsUnauthorized => StatusCode == 401;
    public bool IsForbidden => StatusCode == 403;
    public bool IsNotFound => StatusCode == 404;
}
