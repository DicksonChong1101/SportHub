using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models;

[Index(nameof(FacilityId), nameof(StartAt), nameof(EndAt))]
public class MaintenanceBlock
{
    public int MaintenanceBlockId { get; set; }
    public int FacilityId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    [Required, MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Facility Facility { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User CreatedByUser { get; set; } = null!;
}
