using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public enum NotificationDispatchKind
    {
        NewEntries = 0
    }

    public class NotificationDispatch
    {
        public int Id { get; set; }

        public User User { get; set; }
        public string UserId { get; set; }

        public int Kind { get; set; }
        public DateTime LocalDate { get; set; }
        public DateTime SentAt { get; set; }
    }
}
