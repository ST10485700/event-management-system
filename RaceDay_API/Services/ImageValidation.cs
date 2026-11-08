using Microsoft.AspNetCore.Http;

namespace RaceDay_API.Services;

public static class ImageValidation
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    /// <summary>Returns an error message, or null when the file is an acceptable image.</summary>
    public static string? Validate(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "The image file is empty.";
        }

        if (file.Length > MaxBytes)
        {
            return "The image must be 5 MB or smaller.";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension) || !AllowedTypes.Contains(contentType))
        {
            return "Only JPG, PNG, WEBP or GIF images are allowed.";
        }

        return null;
    }
}
