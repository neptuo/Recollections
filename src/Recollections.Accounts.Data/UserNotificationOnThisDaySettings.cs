namespace Neptuo.Recollections.Accounts
{
    public class UserNotificationOnThisDaySettings
    {
        public User User { get; set; }
        public string UserId { get; set; }

        public bool IsEnabled { get; set; }

        public int PreferredHour { get; set; } = 8;

        public string TimeZone { get; set; } = "UTC";
    }
}
