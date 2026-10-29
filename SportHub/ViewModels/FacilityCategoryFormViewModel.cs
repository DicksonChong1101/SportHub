using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

// Keeps form-only validation separate from the database entity.
public class FacilityCategoryFormViewModel
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Display(Name = "Available for use")]
    public bool IsActive { get; set; } = true;
}
