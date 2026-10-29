using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models;
using SportHub.Services;
using SportHub.ViewModels;

namespace SportHub.Controllers;

// ======================================================
// HOME HERO MODULE - ADMIN BANNER MANAGEMENT
// Only Admin users can create, edit, order, activate or delete banners.
// ======================================================
[Authorize(Roles = UserRoles.Admin)]
public class HeroSlidesController : Controller
{
    private readonly AppDbContext _context;
    private readonly HeroSlideImageService _imageService;

    public HeroSlidesController(
        AppDbContext context,
        HeroSlideImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var slides = await _context.HeroSlides
            .AsNoTracking()
            .OrderBy(slide => slide.DisplayOrder)
            .ThenBy(slide => slide.HeroSlideId)
            .ToListAsync();

        return View(slides);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var nextOrder = await _context.HeroSlides
            .Select(slide => (int?)slide.DisplayOrder)
            .MaxAsync() ?? -1;

        return View(new HeroSlideFormViewModel
        {
            DisplayOrder = Math.Min(nextOrder + 1, 100),
            IsActive = true
        });
    }

    [HttpPost]
    // Allow the browser to submit a large source photo. The client optimizer
    // normally reduces it below 8 MB; server validation remains the final guard.
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        HeroSlideFormViewModel model)
    {
        if (model.Image is null)
        {
            ModelState.AddModelError(
                nameof(model.Image),
                "Select a background image.");
        }
        else
        {
            var validationError =
                await _imageService.ValidateAsync(model.Image);

            if (validationError is not null)
            {
                ModelState.AddModelError(
                    nameof(model.Image),
                    validationError);
            }
        }

        if (!ModelState.IsValid || model.Image is null)
        {
            return View(model);
        }

        HeroStoredImage storedImage;

        try
        {
            storedImage = await _imageService.SaveAsync(model.Image);
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException)
        {
            ModelState.AddModelError(
                nameof(model.Image),
                "The background image could not be saved. Try another image.");

            return View(model);
        }

        var slide = new HeroSlide
        {
            Title = model.Title.Trim(),
            Subtitle = NormalizeOptionalText(model.Subtitle),
            AltText = NormalizeOptionalText(model.AltText),
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            OriginalFileName = storedImage.OriginalFileName,
            StoredFileName = storedImage.StoredFileName,
            ContentType = storedImage.ContentType,
            FileSize = storedImage.FileSize
        };

        _context.HeroSlides.Add(slide);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _imageService.DeleteFile(storedImage.StoredFileName);
            ModelState.AddModelError(
                string.Empty,
                "The Home banner could not be created. Try again.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Home banner created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var slide = await _context.HeroSlides.FindAsync(id);

        if (slide is null)
        {
            return NotFound();
        }

        return View(new HeroSlideFormViewModel
        {
            HeroSlideId = slide.HeroSlideId,
            Title = slide.Title,
            Subtitle = slide.Subtitle,
            AltText = slide.AltText,
            DisplayOrder = slide.DisplayOrder,
            IsActive = slide.IsActive,
            CurrentImagePath =
                HeroSlideImageService.GetDisplayPath(slide.StoredFileName)
        });
    }

    [HttpPost]
    // Keeps Edit resilient when an administrator selects a large replacement.
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        HeroSlideFormViewModel model)
    {
        if (id != model.HeroSlideId)
        {
            return BadRequest();
        }

        var slide = await _context.HeroSlides.FindAsync(id);

        if (slide is null)
        {
            return NotFound();
        }

        if (model.Image is not null)
        {
            var validationError =
                await _imageService.ValidateAsync(model.Image);

            if (validationError is not null)
            {
                ModelState.AddModelError(
                    nameof(model.Image),
                    validationError);
            }
        }

        if (!ModelState.IsValid)
        {
            model.CurrentImagePath =
                HeroSlideImageService.GetDisplayPath(slide.StoredFileName);

            return View(model);
        }

        HeroStoredImage? newImage = null;

        if (model.Image is not null)
        {
            try
            {
                newImage = await _imageService.SaveAsync(model.Image);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                ModelState.AddModelError(
                    nameof(model.Image),
                    "The background image could not be saved. Try another image.");

                model.CurrentImagePath =
                    HeroSlideImageService.GetDisplayPath(slide.StoredFileName);

                return View(model);
            }
        }

        var previousStoredFileName = slide.StoredFileName;

        slide.Title = model.Title.Trim();
        slide.Subtitle = NormalizeOptionalText(model.Subtitle);
        slide.AltText = NormalizeOptionalText(model.AltText);
        slide.DisplayOrder = model.DisplayOrder;
        slide.IsActive = model.IsActive;

        if (newImage is not null)
        {
            slide.OriginalFileName = newImage.OriginalFileName;
            slide.StoredFileName = newImage.StoredFileName;
            slide.ContentType = newImage.ContentType;
            slide.FileSize = newImage.FileSize;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _imageService.DeleteFile(newImage?.StoredFileName);
            ModelState.AddModelError(
                string.Empty,
                "The Home banner could not be updated. Try again.");

            model.CurrentImagePath =
                HeroSlideImageService.GetDisplayPath(previousStoredFileName);

            return View(model);
        }

        if (newImage is not null)
        {
            _imageService.DeleteFile(previousStoredFileName);
        }

        TempData["SuccessMessage"] = "Home banner updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var slide = await _context.HeroSlides
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.HeroSlideId == id);

        return slide is null ? NotFound() : View(slide);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var slide = await _context.HeroSlides.FindAsync(id);

        if (slide is null)
        {
            return NotFound();
        }

        var storedFileName = slide.StoredFileName;
        _context.HeroSlides.Remove(slide);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The Home banner could not be deleted. Try again.";

            return RedirectToAction(nameof(Index));
        }

        _imageService.DeleteFile(storedFileName);

        TempData["SuccessMessage"] = "Home banner deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
