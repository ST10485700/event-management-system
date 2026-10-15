// Models/DbSeeder.cs
namespace RaceDay_API.Models;

public static class DbSeeder
{
    public static void Seed(RaceDayContext db)
    {
        if (db.Users.Any()) return;

        string Hash(string p) => BCrypt.Net.BCrypt.HashPassword(p);

        var thabo = new User { FullName = "Thabo Mokoena", Email = "thabo@raceday.co.za", PasswordHash = Hash("Password123!"), Role = "Organiser" };
        var lerato = new User { FullName = "Lerato Dlamini", Email = "lerato@raceday.co.za", PasswordHash = Hash("Password123!"), Role = "Organiser" };
        var matodzi = new User { FullName = "Matodzi Nevhutala", Email = "matodzi@raceday.co.za", PasswordHash = Hash("Password123!"), Role = "Participant" };
        var uche = new User { FullName = "Uche Brandan", Email = "uche@raceday.co.za", PasswordHash = Hash("Password123!"), Role = "Participant" };

        var e1 = new Event { Organiser = thabo, Name = "Johannesburg City Run", Description = "A road running event in Johannesburg", EventDate = new DateOnly(2026, 10, 10), Location = "Johannesburg", EventType = "Running" };
        var e2 = new Event { Organiser = thabo, Name = "Pretoria Cycle Challenge", Description = "A cycling event for different skill levels", EventDate = new DateOnly(2026, 11, 7), Location = "Pretoria", EventType = "Cycling" };
        var e3 = new Event { Organiser = lerato, Name = "Rosebank Charity Walk", Description = "A community walking event supporting charity", EventDate = new DateOnly(2026, 12, 5), Location = "Rosebank", EventType = "Walking" };

        var c1 = new Category { Event = e1, Name = "5km Fun Run", DistanceKm = 5.00m, MaxParticipants = 500, EntryFee = 100.00m };
        var c2 = new Category { Event = e1, Name = "10km Race", DistanceKm = 10.00m, MaxParticipants = 750, EntryFee = 180.00m };
        var c3 = new Category { Event = e1, Name = "21km Half Marathon", DistanceKm = 21.10m, MaxParticipants = 1000, EntryFee = 300.00m };
        var c4 = new Category { Event = e2, Name = "20km Cycle", DistanceKm = 20.00m, MaxParticipants = 300, EntryFee = 200.00m };
        var c5 = new Category { Event = e2, Name = "50km Cycle", DistanceKm = 50.00m, MaxParticipants = 500, EntryFee = 350.00m };
        var c6 = new Category { Event = e3, Name = "5km Community Walk", DistanceKm = 5.00m, MaxParticipants = 600, EntryFee = 80.00m };
        var c7 = new Category { Event = e3, Name = "10km Charity Walk", DistanceKm = 10.00m, MaxParticipants = 400, EntryFee = 120.00m };

        var routes = new[]
        {
            new Route { Category = c1, RouteName = "City 5km Route", DistanceKm = 5.00m, ElevationGain = 65, MapUrl = "https://example.com/jhb5km" },
            new Route { Category = c2, RouteName = "City 10km Route", DistanceKm = 10.00m, ElevationGain = 120, MapUrl = "https://example.com/jhb10km" },
            new Route { Category = c3, RouteName = "Half Marathon Route", DistanceKm = 21.10m, ElevationGain = 240, MapUrl = "https://example.com/jhb21km" },
            new Route { Category = c4, RouteName = "Pretoria 20km Cycle Route", DistanceKm = 20.00m, ElevationGain = 180, MapUrl = "https://example.com/pta20km" },
            new Route { Category = c5, RouteName = "Pretoria 50km Cycle Route", DistanceKm = 50.00m, ElevationGain = 420, MapUrl = "https://example.com/pta50km" },
            new Route { Category = c6, RouteName = "Rosebank 5km Walk Route", DistanceKm = 5.00m, ElevationGain = 55, MapUrl = "https://example.com/rb5km" },
            new Route { Category = c7, RouteName = "Rosebank 10km Walk Route", DistanceKm = 10.00m, ElevationGain = 90, MapUrl = "https://example.com/rb10km" }
        };

        var en1 = new EventEnrolment { Participant = matodzi, Category = c1, BibNumber = "RD1001" };
        var en2 = new EventEnrolment { Participant = matodzi, Category = c2, BibNumber = "RD1002" };
        var en3 = new EventEnrolment { Participant = uche, Category = c1, BibNumber = "RD1003" };
        var en4 = new EventEnrolment { Participant = uche, Category = c4, BibNumber = "RD1004" };
        var en5 = new EventEnrolment { Participant = matodzi, Category = c6, BibNumber = "RD1005" };

        var results = new[]
        {
            new Result { Enrolment = en1, FinishTime = new TimeSpan(0, 31, 42), Position = 12, CapturedBy = thabo },
            new Result { Enrolment = en2, FinishTime = new TimeSpan(0, 58, 16), Position = 8, CapturedBy = thabo },
            new Result { Enrolment = en3, FinishTime = new TimeSpan(0, 34, 25), Position = 18, CapturedBy = thabo }
        };

        db.AddRange(thabo, lerato, matodzi, uche, e1, e2, e3,
                    c1, c2, c3, c4, c5, c6, c7, en1, en2, en3, en4, en5);
        db.AddRange(routes);
        db.AddRange(results);
        db.SaveChanges();
    }
}