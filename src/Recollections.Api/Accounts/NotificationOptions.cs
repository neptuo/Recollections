namespace Neptuo.Recollections.Accounts.Notifications
{
    public class NotificationOptions
    {
        public string Subject { get; set; } = "mailto:notifications@recollections.app";
        public string PublicKey { get; set; } = "";
        public string PrivateKey { get; set; } = "";
        public string DefaultTimeZoneId { get; set; } = "UTC";
        public int DefaultPreferredHour { get; set; } = 8;
        public int CheckPeriodMinutes { get; set; } = 5;
    }
}
