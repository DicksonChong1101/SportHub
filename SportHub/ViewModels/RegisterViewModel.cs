using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

// Carries registration input; the controller will map it to a Member User entity.
public class RegisterViewModel
{
    [Required]
    [Display(Name = "Full name")]
    // Limits the full name to between 2 and 100 characters.
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required]
    // Tells the Razor View to render a masked password input.
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    // Requires lowercase, uppercase, numeric, and special characters.
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).+$",
        ErrorMessage = "Password must contain an uppercase letter, a lowercase letter, a number, and a special character.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
