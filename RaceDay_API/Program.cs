using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("RaceDayConnection")));

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Session support (stores the logged-in user's ID and role)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Show Users first, then the other controllers alphabetically
    options.OrderActionsBy(api =>
    {
        var controller = api.ActionDescriptor.RouteValues["controller"] ?? "";
        return controller == "Users" ? "0_Users" : "1_" + controller;
    });
});

var app = builder.Build();

// Create the database and seed it on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();        // SQL Server: apply migrations
    }
    else
    {
        db.Database.EnsureCreated();  // in-memory database used by the unit tests
    }
    DbSeeder.Seed(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Lets the test project start the API in memory
public partial class Program { }