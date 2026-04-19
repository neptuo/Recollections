using System;

namespace Neptuo.Recollections.Accounts
{
    public class UserNotificationOnThisDayDispatch
    {
        public int Id { get; set; }

        public User User { get; set; }
        public string UserId { get; set; }

        public DateTime Date { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? SentAt { get; set; }
    }
}
