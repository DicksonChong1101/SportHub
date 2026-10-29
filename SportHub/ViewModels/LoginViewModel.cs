using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
