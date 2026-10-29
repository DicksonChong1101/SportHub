using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

// ======================================================
// ACCOUNT MODULE - PASSWORD RESET TOKEN
// Stores only a hash of the one-time token sent by email.
// ======================================================

[Index(nameof(TokenHash), IsUnique = true)]
public class PasswordResetToken
{
    public int PasswordResetTokenId { get; set; }

    // The User account that requested the password reset.
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    // SHA-256 produces a 64-character hexadecimal hash.
    // The original token is sent by email and is never stored.
    [Required]
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    // The reset link becomes invalid after this UTC time.
    public DateTime ExpiresAt { get; set; }

    // Null means the token has not been used.
    // A value means the password was already reset with this token.
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property:
    // Many PasswordResetTokens can belong to one User.
    public User User { get; set; } = null!;
}
