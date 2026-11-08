using Microsoft.AspNetCore.Http;

namespace RaceDay_API.Services;

/// <summary>Stores uploaded images in cloud storage and returns the public URL.</summary>
public interface IImageStorage
{
    Task<string> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
}

/// <summary>Thrown when an image could not be stored (not configured, or the storage service failed).</summary>
public class ImageStorageException : Exception
{
    public ImageStorageException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
