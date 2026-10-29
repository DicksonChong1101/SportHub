using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// ======================================================
// STUDENT A ADDITIONAL FEATURE - MULTIPLE FACILITY IMAGES
// Uploads a gallery, selects a cover, and removes images.
// ======================================================
[Authorize(Roles = UserRoles.Admin)]
public class FacilityGalleryController : Controller
{
    private const int MaximumImagesPerFacility = 8;
    private const int MaximumImagesPerUpload = 5;

    private readonly AppDbContext _context;
    private readonly FacilityImageService _imageService;

    public FacilityGalleryController(
        AppDbContext context,
        FacilityImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    [HttpGet]
    public async Task<IActionResult> Manage(int facilityId)
    {
        var facility = await LoadFacilityAsync(facilityId);

        return facility is null
            ? NotFound()
            : View(BuildViewModel(facility));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        FacilityGalleryViewModel model)
    {
        var facility = await LoadFacilityAsync(model.FacilityId);

        if (facility is null)
        {
            return NotFound();
        }

        if (model.NewImages.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.NewImages),
                "Select at least one facility image.");
        }
        else if (model.NewImages.Count > MaximumImagesPerUpload)
        {
            ModelState.AddModelError(
                nameof(model.NewImages),
                $"Upload no more than {MaximumImagesPerUpload} images at once.");
        }

        if (facility.FacilityImages.Count + model.NewImages.Count >
            MaximumImagesPerFacility)
        {
            ModelState.AddModelError(
                nameof(model.NewImages),
                $"A facility can store up to {MaximumImagesPerFacility} images.");
        }

        foreach (var image in model.NewImages)
        {
            var validationError = await _imageService.ValidateAsync(image);

            if (validationError is not null)
            {
                ModelState.AddModelError(
                    nameof(model.NewImages),
                    $"{Path.GetFileName(image.FileName)}: {validationError}");
            }
        }

        if (!ModelState.IsValid)
        {
            RestoreViewModel(model, facility);
            return View("Manage", model);
        }

        var savedImages = new List<FacilityImage>();
        var nextOrder = facility.FacilityImages.Count == 0
            ? 0
            : facility.FacilityImages.Max(image => image.DisplayOrder) + 1;

        try
        {
            foreach (var upload in model.NewImages)
            {
                var image = await _imageService.SaveAsync(
                    upload,
                    facility.Name);

                image.IsPrimary =
                    facility.FacilityImages.Count == 0 &&
                    savedImages.Count == 0;
                image.DisplayOrder = Math.Min(nextOrder++, 100);
                facility.FacilityImages.Add(image);
                savedImages.Add(image);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception exception)
            when (exception is IOException or
                  UnauthorizedAccessException or
                  DbUpdateException)
        {
            foreach (var image in savedImages)
            {
                _imageService.DeleteFile(image.StoredFileName);
            }

            ModelState.AddModelError(
                string.Empty,
                "The facility images could not be saved. Try again.");

            RestoreViewModel(model, facility);
            return View("Manage", model);
        }

        TempData["SuccessMessage"] =
            $"{savedImages.Count} facility image(s) uploaded successfully.";

        return RedirectToAction(
            nameof(Manage),
            new { facilityId = facility.FacilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimary(
        int facilityId,
        int imageId)
    {
        var facility = await LoadFacilityAsync(facilityId);

        if (facility is null)
        {
            return NotFound();
        }

        var selectedImage = facility.FacilityImages
            .SingleOrDefault(image =>
                image.FacilityImageId == imageId);

        if (selectedImage is null)
        {
            return NotFound();
        }

        foreach (var image in facility.FacilityImages)
        {
            image.IsPrimary =
                image.FacilityImageId == selectedImage.FacilityImageId;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The Home and booking cover image has been updated.";

        return RedirectToAction(nameof(Manage), new { facilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int facilityId,
        int imageId)
    {
        var facility = await LoadFacilityAsync(facilityId);

        if (facility is null)
        {
            return NotFound();
        }

        var image = facility.FacilityImages
            .SingleOrDefault(candidate =>
                candidate.FacilityImageId == imageId);

        if (image is null)
        {
            return NotFound();
        }

        var storedFileName = image.StoredFileName;
        var wasPrimary = image.IsPrimary;

        _context.FacilityImages.Remove(image);

        if (wasPrimary)
        {
            var replacement = facility.FacilityImages
                .Where(candidate =>
                    candidate.FacilityImageId != imageId)
                .OrderBy(candidate => candidate.DisplayOrder)
                .ThenBy(candidate => candidate.FacilityImageId)
                .FirstOrDefault();

            if (replacement is not null)
            {
                replacement.IsPrimary = true;
            }
        }

        await _context.SaveChangesAsync();
        _imageService.DeleteFile(storedFileName);

        TempData["SuccessMessage"] =
            "Facility image deleted successfully.";

        return RedirectToAction(nameof(Manage), new { facilityId });
    }

    private Task<Facility?> LoadFacilityAsync(int facilityId)
    {
        return _context.Facilities
            .Include(facility => facility.FacilityCategory)
            .Include(facility => facility.FacilityImages)
            .SingleOrDefaultAsync(
                facility => facility.FacilityId == facilityId);
    }

    private static FacilityGalleryViewModel BuildViewModel(
        Facility facility)
    {
        var model = new FacilityGalleryViewModel();
        RestoreViewModel(model, facility);
        return model;
    }

    private static void RestoreViewModel(
        FacilityGalleryViewModel model,
        Facility facility)
    {
        model.FacilityId = facility.FacilityId;
        model.FacilityName = facility.Name;
        model.CategoryName = facility.FacilityCategory.Name;
        model.Images = facility.FacilityImages
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.FacilityImageId)
            .Select(image => new FacilityGalleryImageViewModel
            {
                FacilityImageId = image.FacilityImageId,
                ImagePath = FacilityImageService.GetDisplayPath(image),
                AltText = image.AltText ?? facility.Name,
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder
            })
            .ToList();

        model.NewImages = [];
    }
}

