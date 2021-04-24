using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class MonthView
    {
        [Parameter]
        public int Year { get; set; }

        [Parameter]
        public int Month { get; set; }

        protected int FirstDay { get; set; }
        protected int MaxDay { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            FirstDay = (int)new DateTime(Year, Month, 1).DayOfWeek;
            if (FirstDay == 0)
                FirstDay = 6;
            else
                FirstDay--;

            MaxDay = DateTime.DaysInMonth(Year, Month);
        }
    }
}
