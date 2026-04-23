# SKILL: EXIF Coordinate Validation

**Category:** Data Input Validation / EXIF Parsing  
**Author:** Switch  
**Date Created:** 2026-04-09  
**Applies To:** Image and Video location (GPS) imports

---

## The Pattern

When parsing external EXIF data (GPS coordinates), **always validate that computed float values are finite** before storing to the database. SQL Server and many ORMs reject NaN and Infinity parameters.

## Why It Matters

Corrupted EXIF GPS metadata can produce non-finite floats through:
- Division by zero or malformed EXIF rationals
- Zero-length or truncated GPS arrays
- Extreme values (> 999999) interpreted as large numbers
- ExifLib parsing quirks on invalid byte streams

These non-finite values **silently pass through** arithmetic operations like `Math.Round()` but fail **at database insert time** with cryptic "invalid float parameter" errors.

## The Fix

### Layer 1: EXIF Parsing (ImagePropertyReader)
Validate coordinates before returning them:

```csharp
private double? FindCoordinate(ExifTags type)
{
    if (reader == null)
        return null;

    if (reader.GetTagValue(type, out double[] coordinates))
    {
        // Guard: Check array is long enough
        if (coordinates == null || coordinates.Length < 3)
            return null;
        
        double result = ToDoubleCoordinates(coordinates);
        
        // Guard: Reject non-finite values
        if (!double.IsFinite(result))
            return null;
        
        return result;
    }

    return null;
}
```

**Why this layer:** Catches bad EXIF at the source.

### Layer 2: Bounds Validation (Service)
Create a bounds class parallel to `AltitudeBounds`, then centralize normalization in a shared media-location helper:

```csharp
// CoordinateBounds.cs
public static class CoordinateBounds
{
    public const double LatitudeMin = -90;
    public const double LatitudeMax = 90;
    public const double LongitudeMin = -180;
    public const double LongitudeMax = 180;

    public static bool IsValidLatitude(double latitude)
        => double.IsFinite(latitude) && latitude >= LatitudeMin && latitude <= LatitudeMax;

    public static bool IsValidLongitude(double longitude)
        => double.IsFinite(longitude) && longitude >= LongitudeMin && longitude <= LongitudeMax;
}

// MediaLocationSanitizer.cs
public static class MediaLocationSanitizer
{
    public static void Normalize(MediaLocation location)
    {
        if (location == null)
            return;

        location.Latitude = CoordinateBounds.NormalizeLatitude(location.Latitude);
        location.Longitude = CoordinateBounds.NormalizeLongitude(location.Longitude);
        location.Altitude = AltitudeBounds.IsValid(location.Altitude) ? location.Altitude : null;

        if (location.Latitude == null || location.Longitude == null)
        {
            location.Latitude = null;
            location.Longitude = null;
        }
    }
}
```

Apply in `ImageService.SetProperties()`:

```csharp
entity.Location.Latitude = propertyReader.FindLatitude();
entity.Location.Longitude = propertyReader.FindLongitude();
entity.Location.Altitude = propertyReader.FindAltitude();
MediaLocationSanitizer.Normalize(entity.Location);
```

Use the same `MediaLocationSanitizer.Normalize()` call after `MapModelToEntity()` in image/video services so manual edits cannot persist NaN/Infinity either.

**Why this layer:** Defense in depth—catches coordinate values from any source (EXIF, video container metadata, API edit, etc.).

### Layer 3: Test Coverage
Three regression tests must cover:

1. **EXIF parsing with the regression image** → `FindLatitude()` / `FindLongitude()` return finite values and no EXIF exceptions escape.
2. **Service layer validation** → Invalid coordinates are nullified before save.
3. **Import endpoint regression** → `POST /api/entries/{entryId}/images` succeeds and stores only finite coordinates for the known failing image.

## Files to Update

- `src/Recollections.Entries/ImagePropertyReader.cs` — Add finite check in `FindCoordinate()`.
- `src/Recollections.Entries.Data/CoordinateBounds.cs` — Bounds + normalization helpers.
- `src/Recollections.Entries.Data/MediaLocationSanitizer.cs` — Shared location normalization.
- `src/Recollections.Entries/ImageService.cs` — Apply normalization in `SetProperties()` and `MapModelToEntity()`.
- `src/Recollections.Entries/VideoService.cs` — Same pattern for video metadata and model updates.
- `src/Recollections.Api.Tests/TestData/Images/20260423_073316.jpg` — Real regression asset.
- `src/Recollections.Api.Tests/Entries/*.cs` — Regression coverage.
- `src/Recollections.Api.Tests/Infrastructure/ApiFactory.cs` — Test-only storage/free-limit config for upload scenarios.

## Checklist

- [x] `double.IsFinite()` check on computed coordinates at EXIF parse time.
- [x] `CoordinateBounds` class created with lat/lon min/max constants.
- [x] Shared `MediaLocationSanitizer` normalizes coordinates before storing to entity.
- [x] Regression EXIF read test covers the failing image.
- [x] Import integration test verifies database save succeeds.
- [x] Video service also updated (same pattern).

---

## Key Insight

**Never assume EXIF data is well-formed.** Even "valid" EXIF metadata can contain parser traps. Use `double.IsFinite()` as the final gate before database writes. This is cheaper and clearer than a post-database error.
