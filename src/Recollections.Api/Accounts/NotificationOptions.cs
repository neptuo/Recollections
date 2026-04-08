namespace Neptuo.Recollections.Accounts.Notifications
{
    public class NotificationOptions
    {
        public string Subject { get; set; } = "mailto:notifications@recollections.app";
        public string PublicKey { get; set; } = "";
        public string PrivateKey { get; set; } = "";
    }
}
