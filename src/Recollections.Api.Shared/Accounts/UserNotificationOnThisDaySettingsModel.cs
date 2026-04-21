namespace Neptuo.Recollections.Accounts
{
    public class UserNotificationOnThisDaySettingsModel
    {
        public bool IsEnabled { get; set; }
        public int PreferredHour { get; set; } = 8;
        public string TimeZone { get; set; } = "UTC";
    }
}
