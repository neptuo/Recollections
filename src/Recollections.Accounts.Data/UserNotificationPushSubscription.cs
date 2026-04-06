using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class UserNotificationPushSubscription
    {
        public int Id { get; set; }

        public User User { get; set; }
        public string UserId { get; set; }

        public string Endpoint { get; set; }
        public string P256dh { get; set; }
        public string Auth { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
