using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).+$",
        ErrorMessage = "Password must contain an uppercase letter, a lowercase letter, a number, and a special character.")]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation password do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
