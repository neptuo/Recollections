using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class UserNotificationSettings
    {
        public User User { get; set; }
        public string UserId { get; set; }

        public bool IsEnabled { get; set; }
        public string TimeZoneId { get; set; }
        public int PreferredHour { get; set; }

        public UserNotificationSettings()
        {
            TimeZoneId = "UTC";
            PreferredHour = 8;
        }
    }
}
