using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

// Ensures that an email address can only be registered once.
[Index(nameof(Email), IsUnique = true)]
public class User
{
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    // Validates that the user entered a correctly formatted email address.
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Phone, MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // Stores only the generated avatar file name; the file is kept under wwwroot.
    [MaxLength(255)]
    public string? ProfileImageFileName { get; set; }

    // Limits stored roles to the two roles designed for this system.
    [Required, MaxLength(20)]
    [AllowedValues(UserRoles.Admin, UserRoles.Member)]
    public string Role { get; set; } = UserRoles.Member;

    public bool IsActive { get; set; } = true;

    // New public registrations must prove ownership of their email before login.
    public bool EmailConfirmed { get; set; } = true;

    // Supports temporarily locking an account after repeated failed logins.
    [Range(0, 100)]
    public int FailedLoginAttempts { get; set; }

    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<LoginAttempt> LoginAttempts { get; set; } = [];
    public ICollection<MaintenanceBlock> CreatedMaintenanceBlocks { get; set; } = [];

    // One User may request multiple password-reset links over time.
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = [];
}
