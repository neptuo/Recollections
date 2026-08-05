using System;

namespace Neptuo.Recollections.Accounts
{
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
