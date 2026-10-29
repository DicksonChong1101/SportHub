using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SportHub.ViewModels;

public class MaintenanceBlockFormViewModel : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a facility.")]
    [Display(Name = "Facility")]
    public int FacilityId { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Start date and time")]
    public DateTime? StartAt { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "End date and time")]
    public DateTime? EndAt { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Active closure")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> FacilityOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt.HasValue && EndAt.HasValue && EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "End date and time must be later than the start date and time.",
                [nameof(EndAt)]);
        }
    }
}
