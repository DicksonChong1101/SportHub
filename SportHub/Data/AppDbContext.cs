using Microsoft.EntityFrameworkCore;
using SportHub.Models;

namespace SportHub.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Home banners uploaded and ordered by Admin for the public carousel.
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();

    public DbSet<FacilityCategory> FacilityCategories =>
        Set<FacilityCategory>();

    public DbSet<Facility> Facilities =>
        Set<Facility>();

    public DbSet<User> Users => Set<User>();

    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    public DbSet<FacilityImage> FacilityImages => Set<FacilityImage>();

    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<MaintenanceBlock> MaintenanceBlocks => Set<MaintenanceBlock>();

    // One-time email tokens used by the Forgot Password workflow.
    public DbSet<PasswordResetToken> PasswordResetTokens =>
        Set<PasswordResetToken>();

    // One-time tokens sent after public registration.
    public DbSet<EmailVerificationToken> EmailVerificationTokens =>
        Set<EmailVerificationToken>();
}
