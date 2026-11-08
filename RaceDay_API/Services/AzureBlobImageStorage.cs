using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace RaceDay_API.Services;

/// <summary>
/// Uploads images to Azure Blob Storage.
/// Settings: AzureBlobStorage:ConnectionString (a secret, never committed) and AzureBlobStorage:ContainerName.
/// </summary>
public class AzureBlobImageStorage : IImageStorage
{
    private readonly IConfiguration _configuration;

    public AzureBlobImageStorage(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
        var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ImageStorageException("Image storage is not configured.");
        }

        try
        {
            var container = new BlobContainerClient(connectionString, containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            }, cancellationToken);

            return blob.Uri.ToString();
        }
        catch (RequestFailedException ex)
        {
            throw new ImageStorageException("The image could not be saved to cloud storage.", ex);
        }
        catch (FormatException ex)
        {
            throw new ImageStorageException("Image storage is not configured correctly.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ImageStorageException("Image storage is not configured correctly.", ex);
        }
    }
}
