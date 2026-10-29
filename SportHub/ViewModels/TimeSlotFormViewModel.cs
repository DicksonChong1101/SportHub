using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

public class TimeSlotFormViewModel : IValidatableObject
{
    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Start time")]
    // TimeOnly? lets [Required] distinguish an empty form field from a valid time value.
    public TimeOnly? StartTime { get; set; }

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "End time")]
    public TimeOnly? EndTime { get; set; }

    [Range(0, 100)]
    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Available for booking")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime.HasValue && EndTime.HasValue && EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "End time must be later than start time.",
                [nameof(EndTime)]);
        }
    }
}
