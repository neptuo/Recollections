namespace Neptuo.Recollections.Accounts
{
    public class UserNotificationSettingsModel
    {
        public bool IsEnabled { get; set; }
        public string TimeZoneId { get; set; }
        public int PreferredHour { get; set; }
        public bool HasSubscription { get; set; }
        public string PushPublicKey { get; set; }
        public UserNotificationNewEntriesSettingsModel NewEntries { get; set; }

        public UserNotificationSettingsModel()
        {
            TimeZoneId = "UTC";
            PreferredHour = 8;
            NewEntries = new UserNotificationNewEntriesSettingsModel();
        }
    }
}
