namespace Neptuo.Recollections.Entries
{
    public static class MediaLocationSanitizer
    {
        public static void Set(MediaLocation location, double? latitude, double? longitude, double? altitude)
        {
            if (location == null)
                return;

            location.Latitude = CoordinateBounds.IsValidLatitude(latitude) ? latitude : null;
            location.Longitude = CoordinateBounds.IsValidLongitude(longitude) ? longitude : null;
            location.Altitude = AltitudeBounds.IsValid(altitude) ? altitude : null;
        }

        public static void Normalize(MediaLocation location)
        {
            if (location == null)
                return;

            Set(location, location.Latitude, location.Longitude, location.Altitude);

            if (location.Latitude == null || location.Longitude == null)
            {
                location.Latitude = null;
                location.Longitude = null;
            }
        }
    }
}
