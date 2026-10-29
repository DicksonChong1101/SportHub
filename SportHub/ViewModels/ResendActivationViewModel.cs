using System.ComponentModel.DataAnnotations;

namespace SportHub.ViewModels;

public class ResendActivationViewModel
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;
}
