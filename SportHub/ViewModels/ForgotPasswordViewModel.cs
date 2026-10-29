using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

// ======================================================
// ACCOUNT MODULE - FORGOT PASSWORD FORM
// ======================================================
public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
}

