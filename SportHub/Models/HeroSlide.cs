using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

// ======================================================
// HOME HERO MODULE - DATABASE ENTITY
// Stores each Admin-managed Home banner and its display settings.
// ======================================================
[Index(nameof(DisplayOrder))]
public class HeroSlide
{
    public int HeroSlideId { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Subtitle { get; set; }

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Range(1, 15_728_640)]
    public long FileSize { get; set; }

    [MaxLength(180)]
    [Display(Name = "Alternative text")]
    public string? AltText { get; set; }

    [Range(0, 100)]
    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Show on Home page")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
