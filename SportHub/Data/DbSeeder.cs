using Microsoft.EntityFrameworkCore;
using SportHub.Models;
using SportHub.Services;

namespace SportHub.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        
        await SeedFacilitiesAsync(context);
        await SeedTimeSlotsAsync(context);
    }

   

    private static async Task SeedFacilitiesAsync(AppDbContext context)
    {
        // =====================================================
        // SAMPLE CATALOGUE - ADDITIVE AND SAFE TO RUN REPEATEDLY
        // Existing Admin edits are preserved; only missing named records are added.
        // =====================================================
        var badminton = await EnsureCategoryAsync(
            context, "Badminton", "Indoor badminton courts");
        var futsal = await EnsureCategoryAsync(
            context, "Futsal", "Indoor five-a-side football facilities");
        var football = await EnsureCategoryAsync(
            context, "Football", "Outdoor full-size football fields");
        var basketball = await EnsureCategoryAsync(
            context, "Basketball", "Indoor and outdoor basketball courts");
        var tableTennis = await EnsureCategoryAsync(
            context, "Table Tennis", "Indoor table tennis rooms");
        var gym = await EnsureCategoryAsync(
            context, "Gym", "Indoor fitness and strength training rooms");
        var volleyball = await EnsureCategoryAsync(
            context, "Volleyball", "Indoor volleyball courts");
        var squash = await EnsureCategoryAsync(
            context, "Squash", "Indoor squash courts");
        var tennis = await EnsureCategoryAsync(
            context, "Tennis", "Outdoor tennis courts");

        await EnsureFacilityAsync(context, badminton, "Badminton Court A",
            "Indoor court with wooden flooring", "Sports Centre, Level 1",
            FacilityTypes.Indoor, 4, 20.00m);
        await EnsureFacilityAsync(context, badminton, "Badminton Court B",
            "Indoor court suitable for doubles matches", "Sports Centre, Level 1",
            FacilityTypes.Indoor, 4, 20.00m);

        // Reuses an Indoor Futsal record if the Admin already created one manually.
        var indoorFutsal = await context.Facilities
            .FirstOrDefaultAsync(item =>
                item.FacilityCategoryId == futsal.FacilityCategoryId &&
                item.FacilityType == FacilityTypes.Indoor)
            ?? await EnsureFacilityAsync(context, futsal, "Indoor Futsal Court",
                "Premium indoor artificial-turf court for five-a-side matches", "Indoor Arena, Ground Floor",
                FacilityTypes.Indoor, 14, 90.00m);
        await EnsureSeedImageAsync(context, indoorFutsal,
            "indoor-futsal-court-premium-features1.webp", "seed-indoor-futsal.webp",
            "image/webp", 124846, "Premium indoor futsal court");

        var outdoorFootball = await EnsureFacilityAsync(context, football, "Outdoor Football Field",
            "Floodlit full-size artificial-turf football field", "Outdoor Sports Zone",
            FacilityTypes.Outdoor, 22, 150.00m);
        await EnsureSeedImageAsync(context, outdoorFootball,
            "FootBallCourt_Outdoor.jpeg", "seed-outdoor-football.jpeg",
            "image/jpeg", 133042, "Floodlit outdoor football field");

        await EnsureFacilityAsync(context, basketball, "Indoor Basketball Court",
            "Airy indoor basketball court for training and matches", "Indoor Arena, Level 1",
            FacilityTypes.Indoor, 10, 70.00m);
        // The original sample database already contains an Outdoor Basketball court.
        if (!await context.Facilities.AnyAsync(item =>
                item.FacilityCategoryId == basketball.FacilityCategoryId &&
                item.FacilityType == FacilityTypes.Outdoor))
        {
            await EnsureFacilityAsync(context, basketball, "Outdoor Basketball Court",
                "Full-size outdoor basketball court", "Outdoor Sports Zone",
                FacilityTypes.Outdoor, 10, 60.00m);
        }
        await EnsureFacilityAsync(context, tableTennis, "Table Tennis Room",
            "Indoor room equipped with competition-standard tables", "Sports Centre, Level 2",
            FacilityTypes.Indoor, 8, 18.00m);
        await EnsureFacilityAsync(context, gym, "Gym Room",
            "Fitness room with cardio and strength-training equipment", "Fitness Wing, Level 1",
            FacilityTypes.Indoor, 30, 15.00m);
        await EnsureFacilityAsync(context, volleyball, "Indoor Volleyball Court",
            "Indoor volleyball court suitable for practice and friendly matches", "Indoor Arena, Level 2",
            FacilityTypes.Indoor, 12, 65.00m);
        await EnsureFacilityAsync(context, squash, "Squash Court",
            "Air-conditioned indoor squash court", "Racquet Centre, Level 1",
            FacilityTypes.Indoor, 2, 25.00m);
        await EnsureFacilityAsync(context, tennis, "Outdoor Tennis Court",
            "Outdoor hard court with evening lighting", "Outdoor Racquet Zone",
            FacilityTypes.Outdoor, 4, 45.00m);
    }

    private static async Task<FacilityCategory> EnsureCategoryAsync(
        AppDbContext context,
        string name,
        string description)
    {
        var category = await context.FacilityCategories
            .SingleOrDefaultAsync(item => item.Name == name);

        if (category is not null)
        {
            return category;
        }

        category = new FacilityCategory
        {
            Name = name,
            Description = description,
            IsActive = true
        };

        context.FacilityCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private static async Task<Facility> EnsureFacilityAsync(
        AppDbContext context,
        FacilityCategory category,
        string name,
        string description,
        string location,
        string facilityType,
        int capacity,
        decimal pricePerSlot)
    {
        var facility = await context.Facilities
            .SingleOrDefaultAsync(item => item.Name == name);

        if (facility is not null)
        {
            return facility;
        }

        facility = new Facility
        {
            FacilityCategoryId = category.FacilityCategoryId,
            Name = name,
            Description = description,
            Location = location,
            FacilityType = facilityType,
            Capacity = capacity,
            PricePerSlot = pricePerSlot,
            IsActive = true
        };

        context.Facilities.Add(facility);
        await context.SaveChangesAsync();
        return facility;
    }

    private static async Task EnsureSeedImageAsync(
        AppDbContext context,
        Facility facility,
        string originalFileName,
        string storedFileName,
        string contentType,
        long fileSize,
        string altText)
    {
        if (await context.FacilityImages.AnyAsync(image =>
                image.FacilityId == facility.FacilityId &&
                image.StoredFileName == storedFileName))
        {
            return;
        }

        var existingPrimaryImages = await context.FacilityImages
            .Where(image => image.FacilityId == facility.FacilityId && image.IsPrimary)
            .ToListAsync();

        foreach (var image in existingPrimaryImages)
        {
            image.IsPrimary = false;
        }

        context.FacilityImages.Add(new FacilityImage
        {
            FacilityId = facility.FacilityId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSize = fileSize,
            AltText = altText,
            IsPrimary = true,
            DisplayOrder = 0
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedTimeSlotsAsync(AppDbContext context)
    {
        // Do not create duplicate time slots.
        if (await context.TimeSlots.AnyAsync())
        {
            return;
        }

        context.TimeSlots.AddRange(
            new TimeSlot { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(9, 0), DisplayOrder = 1 },
            new TimeSlot { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), DisplayOrder = 2 },
            new TimeSlot { StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), DisplayOrder = 3 },
            new TimeSlot { StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0), DisplayOrder = 4 },
            new TimeSlot { StartTime = new TimeOnly(15, 0), EndTime = new TimeOnly(16, 0), DisplayOrder = 5 },
            new TimeSlot { StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 0), DisplayOrder = 6 });

        await context.SaveChangesAsync();
    }
}
