using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    // Displays the active facility catalogue to both guests and signed-in users.
    public async Task<IActionResult> Index(string? facilityType = null)
    {
        var selectedFacilityType = facilityType is FacilityTypes.Indoor or FacilityTypes.Outdoor
            ? facilityType
            : null;

        var facilityRecords = await _context.Facilities
            .AsNoTracking()
            .Where(facility =>
                facility.IsActive &&
                facility.FacilityCategory.IsActive)
            .OrderBy(facility => facility.FacilityCategory.Name)
            .ThenBy(facility => facility.Name)
            .Select(facility => new
            {
                facility.FacilityId,
                facility.Name,
                CategoryName = facility.FacilityCategory.Name,
                facility.Location,
                facility.Description,
                facility.Capacity,
                facility.PricePerSlot,
                facility.FacilityType,
                PrimaryImageFileName = facility.FacilityImages
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.DisplayOrder)
                    .Select(image => image.StoredFileName)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // Loads only active banners in the exact order configured by Admin.
        var heroSlideData = await _context.HeroSlides
            .AsNoTracking()
            .Where(slide => slide.IsActive)
            .OrderBy(slide => slide.DisplayOrder)
            .ThenBy(slide => slide.HeroSlideId)
            .Select(slide => new
            {
                slide.HeroSlideId,
                slide.Title,
                slide.Subtitle,
                slide.StoredFileName,
                slide.AltText
            })
            .ToListAsync();

        var heroSlideRecords = heroSlideData
            .Select(slide => new HomeHeroSlideViewModel
            {
                HeroSlideId = slide.HeroSlideId,
                Title = slide.Title,
                Subtitle = slide.Subtitle,
                ImagePath = HeroSlideImageService.GetDisplayPath(
                    slide.StoredFileName),
                AltText = slide.AltText ?? slide.Title
            })
            .ToList();

        // Keeps the Home page usable before Admin uploads the first banner.
        if (heroSlideRecords.Count == 0)
        {
            heroSlideRecords.Add(new HomeHeroSlideViewModel
            {
                Title = "Your game. Your space. Your time.",
                Subtitle =
                    "Reserve quality sports facilities and make every session count.",
                ImagePath = "/images/facilities/sport-hero.svg",
                AltText = "SportHub sports facility"
            });
        }

        var facilityCards = facilityRecords
            .Select(facility => new HomeFacilityCardViewModel
            {
                FacilityId = facility.FacilityId,
                Name = facility.Name,
                CategoryName = facility.CategoryName,
                Location = facility.Location,
                FacilityType = facility.FacilityType,
                Description = facility.Description,
                Capacity = facility.Capacity,
                PricePerSlot = facility.PricePerSlot,
                ImagePath = GetFacilityImagePath(
                    facility.PrimaryImageFileName,
                    facility.CategoryName)
            })
            .ToList();

        if (selectedFacilityType is not null)
        {
            facilityCards = facilityCards
                .Where(facility => facility.FacilityType == selectedFacilityType)
                .ToList();
        }

        // Uses the Admin-selected type to keep Indoor and Outdoor activities together.
        var facilitySections = new[]
        {
            new
            {
                Type = FacilityTypes.Indoor,
                Heading = "Indoor facilities",
                Description = "Comfortable indoor spaces for play in every weather.",
                Icon = "🏟️"
            },
            new
            {
                Type = FacilityTypes.Outdoor,
                Heading = "Outdoor facilities",
                Description = "Open-air courts and pitches for energetic sessions.",
                Icon = "☀️"
            }
        }
        .Select(section => new HomeFacilitySectionViewModel
        {
            FacilityType = section.Type,
            Heading = section.Heading,
            Description = section.Description,
            Icon = section.Icon,
            Facilities = facilityCards
                .Where(facility => facility.FacilityType == section.Type)
                .ToList()
        })
        .Where(section => section.Facilities.Count > 0)
        .ToList();

        var model = new HomeIndexViewModel
        {
            HeroSlides = heroSlideRecords,
            FacilitySections = facilitySections,
            SelectedFacilityType = selectedFacilityType
        };

        return View(model);
    }

    // Explains SportHub's purpose, booking rules, safety and account policies.
    public IActionResult About()
    {
        return View();
    }

    // Preserves old bookmarks while replacing the unfinished Privacy page.
    public IActionResult Privacy()
    {
        return RedirectToAction(nameof(About));
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId =
                Activity.Current?.Id ??
                HttpContext.TraceIdentifier
        });
    }

    // Uses the uploaded primary image when available and a category image otherwise.
    private static string GetFacilityImagePath(
        string? storedFileName,
        string categoryName)
    {
        if (!string.IsNullOrWhiteSpace(storedFileName))
        {
            return $"/uploads/facilities/{Path.GetFileName(storedFileName)}";
        }

        var normalizedCategory = categoryName.ToLowerInvariant();

        if (normalizedCategory.Contains("badminton"))
        {
            return "/images/facilities/badminton.svg";
        }

        if (normalizedCategory.Contains("basketball"))
        {
            return "/images/facilities/basketball.svg";
        }

        if (normalizedCategory.Contains("futsal") ||
            normalizedCategory.Contains("football") ||
            normalizedCategory.Contains("soccer"))
        {
            return "/images/facilities/futsal.svg";
        }

        if (normalizedCategory.Contains("swim"))
        {
            return "/images/facilities/swimming.svg";
        }

        return "/images/facilities/sport-default.svg";
    }
}
