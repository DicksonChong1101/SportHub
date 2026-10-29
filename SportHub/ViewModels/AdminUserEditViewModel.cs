using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

// ======================================================
// ADMIN USER MODULE - EDIT ACCOUNT
// ======================================================
public class AdminUserEditViewModel
{
    public int UserId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Account active")]
    public bool IsActive { get; set; }
}

