using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Neptuo.Recollections.Tests.TestData.Images;

/// <summary>
/// Generates synthetic JPEG fixtures with controlled EXIF GPS data for testing coordinate sanitization.
/// </summary>
public static class SyntheticExifImageGenerator
{
    /// <summary>
    /// Creates a 100x100 black JPEG with synthetic GPS coordinates.
    /// </summary>
    /// <param name="outputPath">The path where the JPEG should be written.</param>
    /// <param name="latitude">GPS latitude in decimal degrees (default: 10.0).</param>
    /// <param name="longitude">GPS longitude in decimal degrees (default: 20.0).</param>
    /// <param name="altitude">GPS altitude in meters (default: 100.0).</param>
    public static void GenerateFixture(
        string outputPath,
        double latitude = 10.0,
        double longitude = 20.0,
        double altitude = 100.0)
    {
        using var image = new Image<Rgba32>(100, 100);
        
        // Fill with black
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                image[x, y] = Color.Black;
            }
        }

        var exif = image.Metadata.ExifProfile ?? new ExifProfile();

        // Convert decimal degrees to degrees, minutes, seconds format required by EXIF
        exif.SetValue(ExifTag.GPSLatitude, ConvertToRational(Math.Abs(latitude)));
        exif.SetValue(ExifTag.GPSLatitudeRef, latitude >= 0 ? "N" : "S");
        exif.SetValue(ExifTag.GPSLongitude, ConvertToRational(Math.Abs(longitude)));
        exif.SetValue(ExifTag.GPSLongitudeRef, longitude >= 0 ? "E" : "W");

        // Altitude is stored as a rational (numerator/denominator) in meters
        exif.SetValue(ExifTag.GPSAltitude, new Rational((uint)(altitude * 100), 100));
        exif.SetValue(ExifTag.GPSAltitudeRef, (byte)0); // 0 = above sea level

        image.Metadata.ExifProfile = exif;

        var encoder = new JpegEncoder { Quality = 75 };
        image.Save(outputPath, encoder);
    }

    private static Rational[] ConvertToRational(double decimalDegrees)
    {
        var degrees = (uint)Math.Floor(decimalDegrees);
        var minutesDecimal = (decimalDegrees - degrees) * 60;
        var minutes = (uint)Math.Floor(minutesDecimal);
        var secondsDecimal = (minutesDecimal - minutes) * 60;
        
        // Store seconds as rational with precision
        var secondsNumerator = (uint)Math.Round(secondsDecimal * 10000);
        var secondsDenominator = 10000u;

        return new[]
        {
            new Rational(degrees, 1),
            new Rational(minutes, 1),
            new Rational(secondsNumerator, secondsDenominator)
        };
    }
}
