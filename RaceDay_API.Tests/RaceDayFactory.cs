using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay_API.Models;

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
        });
    }
}