using Microsoft.AspNetCore.Http;

namespace SportHub.Services;

// ======================================================
// HOME HERO MODULE - SECURE IMAGE STORAGE SERVICE
// Validates image signatures and stores files under randomized names.
// ======================================================
public class HeroSlideImageService
{
    private const long MaxHeroImageBytes = 8 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp"];

    private readonly IWebHostEnvironment _environment;

    public HeroSlideImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string?> ValidateAsync(IFormFile image)
    {
        if (image.Length == 0 || image.Length > MaxHeroImageBytes)
        {
            return "Select an image smaller than 8 MB.";
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

    public async Task<HeroStoredImage> SaveAsync(IFormFile image)
    {
        var extension =
            Path.GetExtension(image.FileName).ToLowerInvariant();

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var uploadDirectory = Path.Combine(
            GetWebRootPath(),
            "uploads",
            "hero-slides");

        Directory.CreateDirectory(uploadDirectory);

        var filePath = Path.Combine(uploadDirectory, storedFileName);
        await using var outputStream = System.IO.File.Create(filePath);
        await image.CopyToAsync(outputStream);

        return new HeroStoredImage(
            Truncate(Path.GetFileName(image.FileName), 255),
            storedFileName,
            GetTrustedContentType(extension),
            image.Length);
    }

    public void DeleteFile(string? storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return;
        }

        var filePath = Path.Combine(
            GetWebRootPath(),
            "uploads",
            "hero-slides",
            Path.GetFileName(storedFileName));

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    public static string GetDisplayPath(string storedFileName)
    {
        return $"/uploads/hero-slides/{Path.GetFileName(storedFileName)}";
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

public sealed record HeroStoredImage(
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long FileSize);
