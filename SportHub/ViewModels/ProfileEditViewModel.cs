using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SportHub.ViewModels;

public class ProfileEditViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Profile image")]
    public IFormFile? ProfileImage { get; set; }

    public string CurrentProfileImagePath { get; set; } = string.Empty;
}
