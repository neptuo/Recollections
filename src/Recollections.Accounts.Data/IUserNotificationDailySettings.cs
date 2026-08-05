using System;

namespace Neptuo.Recollections.Accounts
{
    /// <summary>
    /// Settings of a notification topic that is evaluated once per user's local day.
    /// </summary>
    public interface IUserNotificationDailySettings
    {
        string UserId { get; set; }
        bool IsEnabled { get; set; }
        int PreferredHour { get; set; }
        string TimeZone { get; set; }
    }

    /// <summary>
    /// Dedupe record of a notification topic that is delivered once per user's local day.
    /// </summary>
    public interface IUserNotificationDailyDispatch
    {
        string UserId { get; set; }
        DateTime Date { get; set; }
        DateTime Created { get; set; }
        DateTime? SentAt { get; set; }
    }
}
