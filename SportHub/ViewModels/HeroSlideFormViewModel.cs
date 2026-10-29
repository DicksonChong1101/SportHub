using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SportHub.ViewModels;

// ======================================================
// HOME HERO MODULE - ADMIN CREATE AND EDIT FORM
// Keeps uploaded form data separate from the HeroSlide entity.
// ======================================================
public class HeroSlideFormViewModel
{
    public int HeroSlideId { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 3)]
    [Display(Name = "Headline")]
    public string Title { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Subtitle { get; set; }

    [StringLength(180)]
    [Display(Name = "Alternative text")]
    public string? AltText { get; set; }

    [Range(0, 100)]
    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Show on Home page")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Background image")]
    public IFormFile? Image { get; set; }

    public string CurrentImagePath { get; set; } = string.Empty;
}
