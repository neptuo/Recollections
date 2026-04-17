namespace Neptuo.Recollections.Entries
{
    /// <summary>
    /// Plausible altitude range on Earth in meters.
    /// Used to validate imported altitudes (e.g. from photo EXIF) so that
    /// clearly incorrect values can be detected and unset.
    /// </summary>
    public static class AltitudeBounds
    {
        /// <summary>
        /// Challenger Deep in the Mariana Trench — the deepest place on Earth.
        /// </summary>
        public const double MinMeters = -10935;

        /// <summary>
        /// Summit of Mount Everest, rounded up.
        /// </summary>
        public const double MaxMeters = 8849;

        public static bool IsValid(double altitude)
            => altitude >= MinMeters && altitude <= MaxMeters;

        public static bool IsValid(double? altitude)
            => altitude == null || IsValid(altitude.Value);
    }
}
