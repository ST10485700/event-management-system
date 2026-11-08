using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay_API.Models;
using RaceDay_API.Services;

namespace RaceDay_API.Tests;

public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "RaceDayTests_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var existing = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));
            if (existing != null)
            {
                services.Remove(existing);
            }

            services.AddDbContext<RaceDayContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Tests never talk to Azure: uploads go to a fake that just returns a URL.
            services.RemoveAll<IImageStorage>();
            services.AddScoped<IImageStorage, FakeImageStorage>();
        });
    }
}

public class FakeImageStorage : IImageStorage
{
    public Task<string> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var url = $"https://fake-storage.test/{folder}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        return Task.FromResult(url);
    }
}
