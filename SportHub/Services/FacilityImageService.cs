using Microsoft.AspNetCore.Http;
using SportHub.Models;

namespace SportHub.Services;

public class FacilityImageService
{
    private const long MaxFacilityImageBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp"];

    private readonly IWebHostEnvironment _environment;

    public FacilityImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // Validates the declared extension, size and real binary image signature.
    public async Task<string?> ValidateAsync(IFormFile image)
    {
        if (image.Length == 0 || image.Length > MaxFacilityImageBytes)
        {
            return "Select an image smaller than 5 MB.";
        }

        var extension =
            Path.GetExtension(image.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return "Only JPG, PNG and WebP images are allowed.";
        }

        var signature = new byte[12];
        await using var stream = image.OpenReadStream();
        var bytesRead = await stream.ReadAsync(
            signature.AsMemory(0, signature.Length));

        return HasSupportedImageSignature(signature, bytesRead)
            ? null
            : "The selected file is not a valid JPG, PNG or WebP image.";
    }

    // Saves a randomized file name and returns the matching database entity.
    public async Task<FacilityImage> SaveAsync(
        IFormFile image,
        string facilityName)
    {
        var extension =
            Path.GetExtension(image.FileName).ToLowerInvariant();

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var uploadDirectory = Path.Combine(
            GetWebRootPath(),
            "uploads",
            "facilities");

        Directory.CreateDirectory(uploadDirectory);

        var filePath = Path.Combine(uploadDirectory, storedFileName);
        await using var outputStream = System.IO.File.Create(filePath);
        await image.CopyToAsync(outputStream);

        return new FacilityImage
        {
            OriginalFileName = Truncate(
                Path.GetFileName(image.FileName),
                255),
            StoredFileName = storedFileName,
            ContentType = GetTrustedContentType(extension),
            FileSize = image.Length,
            AltText = Truncate($"{facilityName} cover image", 150),
            IsPrimary = true,
            DisplayOrder = 0
        };
    }

    public void DeleteFile(string? storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return;
        }

        var safeFileName = Path.GetFileName(storedFileName);
        var filePath = Path.Combine(
            GetWebRootPath(),
            "uploads",
            "facilities",
            safeFileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    public static string GetDisplayPath(
        FacilityImage? image,
        string? categoryName = null)
    {
        if (image is not null)
        {
            return $"/uploads/facilities/{Path.GetFileName(image.StoredFileName)}";
        }

        return GetFallbackImagePath(categoryName);
    }

    // Supplies a themed image until an administrator uploads a custom cover.
    public static string GetFallbackImagePath(string? categoryName)
    {
        var normalizedCategory =
            categoryName?.ToLowerInvariant() ?? string.Empty;

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

    private string GetWebRootPath()
    {
        return _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
    }

    private static string GetTrustedContentType(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static bool HasSupportedImageSignature(
        byte[] signature,
        int bytesRead)
    {
        var isJpeg = bytesRead >= 3 &&
                     signature[0] == 0xFF &&
                     signature[1] == 0xD8 &&
                     signature[2] == 0xFF;

        var isPng = bytesRead >= 8 &&
                    signature.AsSpan(0, 8).SequenceEqual(
                        new byte[]
                        {
                            0x89, 0x50, 0x4E, 0x47,
                            0x0D, 0x0A, 0x1A, 0x0A
                        });

        var isWebP = bytesRead >= 12 &&
                     signature.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                     signature.AsSpan(8, 4).SequenceEqual("WEBP"u8);

        return isJpeg || isPng || isWebP;
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}
