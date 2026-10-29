using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class FacilityImagesController : Controller
{
    private readonly AppDbContext _context;
    private readonly FacilityImageService _imageService;

    public FacilityImagesController(
        AppDbContext context,
        FacilityImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    // Displays the current Home-page cover for one facility.
    [HttpGet]
    public async Task<IActionResult> ManageCover(int facilityId)
    {
        var facility = await LoadFacilityAsync(facilityId);

        if (facility is null)
        {
            return NotFound();
        }

        return View(BuildViewModel(facility));
    }

    // Uploads, replaces or removes the facility's primary display image.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageCover(
        FacilityCoverImageViewModel model)
    {
        var facility = await LoadFacilityAsync(model.FacilityId);

        if (facility is null)
        {
            return NotFound();
        }

        var currentCover = GetCurrentCover(facility);

        if (model.CoverImage is null && !model.RemoveCoverImage)
        {
            ModelState.AddModelError(
                nameof(model.CoverImage),
                "Select a new image or choose to remove the current cover.");
        }

        if (model.RemoveCoverImage && currentCover is null)
        {
            ModelState.AddModelError(
                nameof(model.RemoveCoverImage),
                "This facility does not currently have a custom cover.");
        }

        if (model.CoverImage is not null)
        {
            var validationError =
                await _imageService.ValidateAsync(model.CoverImage);

            if (validationError is not null)
            {
                ModelState.AddModelError(
                    nameof(model.CoverImage),
                    validationError);
            }
        }

        if (!ModelState.IsValid)
        {
            RestoreDisplayValues(model, facility);
            return View(model);
        }

        FacilityImage? newCover = null;
        string? oldCoverFileName = null;

        if (model.CoverImage is not null)
        {
            try
            {
                newCover = await _imageService.SaveAsync(
                    model.CoverImage,
                    facility.Name);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                ModelState.AddModelError(
                    nameof(model.CoverImage),
                    "The cover image could not be saved. Try another image.");

                RestoreDisplayValues(model, facility);
                return View(model);
            }

            foreach (var image in facility.FacilityImages)
            {
                image.IsPrimary = false;
            }

            if (currentCover is not null)
            {
                oldCoverFileName = currentCover.StoredFileName;
                _context.FacilityImages.Remove(currentCover);
            }

            facility.FacilityImages.Add(newCover);
        }
        else if (model.RemoveCoverImage && currentCover is not null)
        {
            oldCoverFileName = currentCover.StoredFileName;
            _context.FacilityImages.Remove(currentCover);

            // Future multiple-image support: promote the next image if one exists.
            var replacementCover = facility.FacilityImages
                .Where(image => image != currentCover)
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.FacilityImageId)
                .FirstOrDefault();

            if (replacementCover is not null)
            {
                replacementCover.IsPrimary = true;
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _imageService.DeleteFile(newCover?.StoredFileName);

            ModelState.AddModelError(
                string.Empty,
                "The display cover could not be updated. Try again.");

            RestoreDisplayValues(model, facility);
            return View(model);
        }

        _imageService.DeleteFile(oldCoverFileName);

        TempData["SuccessMessage"] =
            "Facility display cover updated successfully.";

        return RedirectToAction(
            "Details",
            "Facilities",
            new { id = facility.FacilityId });
    }

    private Task<Facility?> LoadFacilityAsync(int facilityId)
    {
        return _context.Facilities
            .Include(facility => facility.FacilityCategory)
            .Include(facility => facility.FacilityImages)
            .SingleOrDefaultAsync(
                facility => facility.FacilityId == facilityId);
    }

    private static FacilityImage? GetCurrentCover(Facility facility)
    {
        return facility.FacilityImages
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.FacilityImageId)
            .FirstOrDefault();
    }

    private static FacilityCoverImageViewModel BuildViewModel(
        Facility facility)
    {
        var currentCover = GetCurrentCover(facility);

        return new FacilityCoverImageViewModel
        {
            FacilityId = facility.FacilityId,
            FacilityName = facility.Name,
            CategoryName = facility.FacilityCategory.Name,
            CurrentImagePath = currentCover is null
                ? GetFallbackImagePath(facility.FacilityCategory.Name)
                : FacilityImageService.GetDisplayPath(currentCover),
            HasCustomCover = currentCover is not null
        };
    }

    private static void RestoreDisplayValues(
        FacilityCoverImageViewModel model,
        Facility facility)
    {
        var currentCover = GetCurrentCover(facility);

        model.FacilityName = facility.Name;
        model.CategoryName = facility.FacilityCategory.Name;
        model.CurrentImagePath = currentCover is null
            ? GetFallbackImagePath(facility.FacilityCategory.Name)
            : FacilityImageService.GetDisplayPath(currentCover);
        model.HasCustomCover = currentCover is not null;
    }

    private static string GetFallbackImagePath(string categoryName)
    {
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
