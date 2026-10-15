// Models/RaceDayContext.cs
using Microsoft.EntityFrameworkCore;

namespace RaceDay_API.Models;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventEnrolment> EventEnrolments => Set<EventEnrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Route> Routes => Set<Route>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Role).HasDefaultValue("Participant");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("getdate()");
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Role",
                "[Role] in ('Organiser', 'Participant')"));
        });

        b.Entity<Event>(e =>
        {
            e.HasOne(x => x.Organiser).WithMany(u => u.Events)
                .HasForeignKey(x => x.OrganiserID).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Events_EventType",
                "[EventType] in ('Running', 'Cycling', 'Walking')"));
        });

        b.Entity<Category>(e =>
        {
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.EventID, x.Name }).IsUnique();
            e.Property(x => x.EntryFee).HasDefaultValue(0.00m);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Categories_DistanceKm", "[DistanceKm] > 0");
                t.HasCheckConstraint("CK_Categories_MaxParticipants", "[MaxParticipants] > 0");
                t.HasCheckConstraint("CK_Categories_EntryFee", "[EntryFee] >= 0");
            });
        });

        b.Entity<EventEnrolment>(e =>
        {
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();
            e.HasIndex(x => x.BibNumber).IsUnique();
            e.Property(x => x.Status).HasDefaultValue("Active");
            e.Property(x => x.EnrolmentDate).HasDefaultValueSql("getdate()");
            e.ToTable(t => t.HasCheckConstraint("CK_EventEnrolments_Status",
                "[Status] in ('Active', 'Cancelled', 'Completed')"));
        });

        b.Entity<Result>(e =>
        {
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentID).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CapturedBy).WithMany()
                .HasForeignKey(x => x.CapturedByUserID).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CapturedAt).HasDefaultValueSql("getdate()");
            e.ToTable(t => t.HasCheckConstraint("CK_Results_Position", "[Position] > 0"));
        });

        b.Entity<Route>(e =>
        {
            e.HasOne(x => x.Category).WithMany(c => c.Routes)
                .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ElevationGain).HasDefaultValue(0);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Routes_DistanceKm", "[DistanceKm] > 0");
                t.HasCheckConstraint("CK_Routes_ElevationGain", "[ElevationGain] >= 0");
            });
        });
    }
}