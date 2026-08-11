using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class Being : IOwnerByUser, ISharingInherited
    {
        [Key]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Name { get; set; }
        public string Icon { get; set; }
        public string Text { get; set; }

        public DateTime? BirthDate { get; set; }

        public int? NameDayMonth { get; set; }
        public int? NameDayDay { get; set; }

        public bool HasValidNameDay()
        {
            if (!NameDayMonth.HasValue && !NameDayDay.HasValue)
                return true;

            if (!NameDayMonth.HasValue || !NameDayDay.HasValue)
                return false;

            return NameDayMonth >= 1
                && NameDayMonth <= 12
                && NameDayDay >= 1
                && NameDayDay <= DateTime.DaysInMonth(2000, NameDayMonth.Value);
        }

        public DateTime Created { get; set; }

        public IList<Entry> Entries { get; set; } = new List<Entry>();

        public bool IsSharingInherited { get; set; }
    }
}
