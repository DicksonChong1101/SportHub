using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

// =====================================================
// STUDENT A ADDITIONAL FEATURE - EMAIL ACCOUNT ACTIVATION
// Only a SHA-256 token hash is stored; the one-time token
// itself appears only in the email activation link.
// =====================================================
[Index(nameof(TokenHash), IsUnique = true)]
public class EmailVerificationToken
{
    public int EmailVerificationTokenId { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    [Required, StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
