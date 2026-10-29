namespace SportHub.ViewModels;

// Combines the rotating Home banners and public facility catalogue in one page model.
public class HomeIndexViewModel
{
    public IReadOnlyList<HomeHeroSlideViewModel> HeroSlides { get; set; } = [];
    public IReadOnlyList<HomeFacilitySectionViewModel> FacilitySections { get; set; } = [];
    public int FacilityCount => FacilitySections.Sum(section => section.Facilities.Count);
    public string? SelectedFacilityType { get; set; }
}

// One Home section represents a real facility environment such as Indoor or Outdoor.
public class HomeFacilitySectionViewModel
{
    public string FacilityType { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public IReadOnlyList<HomeFacilityCardViewModel> Facilities { get; set; } = [];
}

public class HomeHeroSlideViewModel
{
    public int HeroSlideId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
}

public class HomeFacilityCardViewModel
{
    public int FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal PricePerSlot { get; set; }
    public string ImagePath { get; set; } = string.Empty;
}
