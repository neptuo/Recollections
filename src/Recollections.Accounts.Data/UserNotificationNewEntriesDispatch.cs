using System;

namespace Neptuo.Recollections.Accounts
{
    public class UserNotificationNewEntriesDispatch
    {
        public int Id { get; set; }

        public User User { get; set; }
        public string UserId { get; set; }

        public string EntryId { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? SentAt { get; set; }
    }
}
