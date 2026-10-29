using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SportHub.ViewModels;

public class FacilityCoverImageViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CurrentImagePath { get; set; } = string.Empty;
    public bool HasCustomCover { get; set; }

    [Display(Name = "New display cover")]
    public IFormFile? CoverImage { get; set; }

    [Display(Name = "Remove current custom cover")]
    public bool RemoveCoverImage { get; set; }
}
