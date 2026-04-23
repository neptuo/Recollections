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
Create a bounds class parallel to `AltitudeBounds`:

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
```

Apply in `ImageService.SetProperties()`:

```csharp
entity.Location.Latitude = propertyReader.FindLatitude();
entity.Location.Longitude = propertyReader.FindLongitude();

if (entity.Location.Latitude.HasValue && !CoordinateBounds.IsValidLatitude(entity.Location.Latitude.Value))
    entity.Location.Latitude = null;
if (entity.Location.Longitude.HasValue && !CoordinateBounds.IsValidLongitude(entity.Location.Longitude.Value))
    entity.Location.Longitude = null;
```

**Why this layer:** Defense in depth—catches coordinate values from any source (EXIF, API edit, etc.).

### Layer 3: Test Coverage
Three regression tests must cover:

1. **EXIF parsing with corrupted arrays** → `FindLatitude()` returns `null` or valid value, never NaN.
2. **Service layer validation** → Invalid coordinates are nullified before save.
3. **Manual API edits** → User-submitted coordinates are bounds-checked.

## Files to Update

- `src/Recollections.Entries/ImagePropertyReader.cs` — Add finite check in `FindCoordinate()`.
- `src/Recollections.Entries.Data/CoordinateBounds.cs` — Create new bounds class.
- `src/Recollections.Entries/ImageService.cs` — Apply bounds validation in `SetProperties()`.
- `src/Recollections.Entries/VideoService.cs` — Same pattern (Video also imports location).

## Checklist

- [ ] `double.IsFinite()` check on computed coordinates at EXIF parse time.
- [ ] `CoordinateBounds` class created with lat/lon min/max constants.
- [ ] Service layer applies bounds validation before storing to entity.
- [ ] Test: Corrupted EXIF GPS → returns null.
- [ ] Test: Non-finite coordinate in model → nullified before save.
- [ ] Test: Integration test verifies database save succeeds.
- [ ] Video service also updated (same pattern).

---

## Key Insight

**Never assume EXIF data is well-formed.** Even "valid" EXIF metadata can contain parser traps. Use `double.IsFinite()` as the final gate before database writes. This is cheaper and clearer than a post-database error.
