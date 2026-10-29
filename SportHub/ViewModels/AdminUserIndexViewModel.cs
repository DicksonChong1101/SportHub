namespace SportHub.ViewModels;

// ======================================================
// ADMIN USER MODULE - SEARCH, FILTER AND LIST
// ======================================================
public class AdminUserIndexViewModel
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public IReadOnlyList<AdminUserListItemViewModel> Users { get; set; } = [];
}

public class AdminUserListItemViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int ReservationCount { get; set; }
}
