namespace Neptuo.Recollections.Accounts
{
    public class UserNotificationSettingsModel
    {
        public bool IsEnabled { get; set; }
        public bool HasSubscription { get; set; }
        public string PushPublicKey { get; set; }
        public UserNotificationNewEntriesSettingsModel NewEntries { get; set; }

        public UserNotificationSettingsModel()
        {
            NewEntries = new UserNotificationNewEntriesSettingsModel();
        }
    }
}
