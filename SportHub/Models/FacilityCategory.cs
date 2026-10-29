using System.ComponentModel.DataAnnotations;

namespace SportHub.Models;

[Microsoft.EntityFrameworkCore.Index(nameof(Name), IsUnique = true)]
public class FacilityCategory
{
    public int FacilityCategoryId { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Facility> Facilities { get; set; } = [];
}
