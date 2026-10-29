using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportHub.Models;

[Microsoft.EntityFrameworkCore.Index(nameof(Name), IsUnique = true)]
public class Facility
{
    // Primary key for a facility record.
    public int FacilityId { get; set; }

    // Facility name shown to users; the class-level index prevents duplicates.
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Optional description; the nullable marker allows a database NULL value.
    [MaxLength(500)]
    public string? Description { get; set; }

    // Physical location of the facility.
    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    // Admin chooses where the activity is held so Home can group it correctly.
    [Required, MaxLength(20)]
    public string FacilityType { get; set; } = FacilityTypes.Indoor;

    // Accepts a practical capacity from 1 to 1,000 people.
    [Range(1, 1000)]
    public int Capacity { get; set; }

    // Price charged for one reservation time slot.
    [Range(0.01, 9999.99)]
    [Microsoft.EntityFrameworkCore.Precision(10, 2)]
    public decimal PricePerSlot { get; set; }

    public bool IsActive { get; set; } = true;

    // Records when the facility record was created.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key on the many side of the FacilityCategory relationship.
    public int FacilityCategoryId { get; set; }

    [Microsoft.EntityFrameworkCore.DeleteBehavior(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict)]
    public FacilityCategory FacilityCategory { get; set; } = null!;

    // One facility can have many uploaded images.
    public ICollection<FacilityImage> FacilityImages { get; set; } = [];

    public ICollection<Reservation> Reservations { get; set; } = [];

    public ICollection<MaintenanceBlock> MaintenanceBlocks { get; set; } = [];
}
