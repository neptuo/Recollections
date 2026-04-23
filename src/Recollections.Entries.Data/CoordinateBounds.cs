namespace Neptuo.Recollections.Entries
{
    /// <summary>
    /// Plausible latitude/longitude ranges on Earth.
    /// Used to validate imported coordinates (e.g. from media metadata) so that
    /// clearly incorrect values can be detected and unset.
    /// </summary>
    public static class CoordinateBounds
    {
        public const double LatitudeMin = -90;
        public const double LatitudeMax = 90;
        public const double LongitudeMin = -180;
        public const double LongitudeMax = 180;

        public static bool IsValidLatitude(double latitude)
            => double.IsFinite(latitude) && latitude >= LatitudeMin && latitude <= LatitudeMax;

        public static bool IsValidLatitude(double? latitude)
            => latitude == null || IsValidLatitude(latitude.Value);

        public static bool IsValidLongitude(double longitude)
            => double.IsFinite(longitude) && longitude >= LongitudeMin && longitude <= LongitudeMax;

        public static bool IsValidLongitude(double? longitude)
            => longitude == null || IsValidLongitude(longitude.Value);

        public static double? NormalizeLatitude(double? latitude)
            => IsValidLatitude(latitude) ? latitude : null;

        public static double? NormalizeLongitude(double? longitude)
            => IsValidLongitude(longitude) ? longitude : null;
    }
}
