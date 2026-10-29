using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SportHub.ViewModels;

// ======================================================
// FACILITY IMAGE MODULE - MULTIPLE IMAGE GALLERY
// ======================================================
public class FacilityGalleryViewModel
{
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    [Display(Name = "Facility images")]
    public List<IFormFile> NewImages { get; set; } = [];

    public IReadOnlyList<FacilityGalleryImageViewModel> Images { get; set; } = [];
}

public class FacilityGalleryImageViewModel
{
    public int FacilityImageId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}

