using System;

namespace Neptuo.Recollections.Accounts.Notifications
{
    public class NotificationOptions
    {
        public string Subject { get; set; } = "";
        public string PublicKey { get; set; } = "";
        public string PrivateKey { get; set; } = "";

        public OnThisDayNotificationOptions OnThisDay { get; set; } = new();
    }

    public class OnThisDayNotificationOptions
    {
        public TimeSpan TickInterval { get; set; }

        /// <summary>
        /// Offset added to the current UTC clock when the notifier evaluates user
        /// preferences. Intended for development and manual end-to-end validation
        /// only; must stay at <see cref="TimeSpan.Zero"/> in production.
        /// </summary>
        public TimeSpan ClockOffset { get; set; } = TimeSpan.Zero;
    }
}

