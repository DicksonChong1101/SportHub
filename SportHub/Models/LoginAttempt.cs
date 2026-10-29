using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

[Index(nameof(AttemptedAt))]
public class LoginAttempt
{
    public int LoginAttemptId { get; set; }
    public int? UserId { get; set; }

    [Required, EmailAddress, MaxLength(254)]
    public string EmailAttempted { get; set; } = string.Empty;

    // Records whether the login attempt succeeded or failed.
    public bool IsSuccessful { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    // Stores the source IP address; 45 characters can accommodate IPv6.
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    // Stores information about the browser and device used for the attempt.
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    // Examples include invalid password, account not found, or account locked.
    [MaxLength(100)]
    public string? FailureReason { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User? User { get; set; }
}
