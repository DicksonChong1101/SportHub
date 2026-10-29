using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportHub.Models;

namespace SportHub.ViewModels;

public class FacilityFormViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Facility name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        "^(Indoor|Outdoor)$",
        ErrorMessage = "Select Indoor or Outdoor.")]
    [Display(Name = "Facility type")]
    public string FacilityType { get; set; } = FacilityTypes.Indoor;

    [Range(1, 1000)]
    public int Capacity { get; set; } = 1;

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Display(Name = "Price per slot")]
    public decimal PricePerSlot { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a facility category.")]
    [Display(Name = "Facility category")]
    public int FacilityCategoryId { get; set; }

    [Display(Name = "Available for booking")]
    public bool IsActive { get; set; } = true;

    // Supplies the category dropdown without adding UI-only data to the Facility entity.
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];
}
