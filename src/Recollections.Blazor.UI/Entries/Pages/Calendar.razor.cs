using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class Calendar
    {
        [Parameter]
        public int? Year { get; set; }

        [Parameter]
        public int? Month { get; set; }

        protected bool IsMonthView => Month != null;

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (Year == null)
            {
                Year = DateTime.Now.Year;
                Month = DateTime.Now.Month;
            }
        }

        protected void PrevPeriod()
        {
            if (IsMonthView)
            {
                if (Month > 0)
                {
                    Month--;
                }
                else
                {
                    Year--;
                    Month = 12;
                }
            }
        }

        protected void NextPeriod()
        {
            if (IsMonthView)
            {
                if (Month < 12)
                {
                    Month++;
                }
                else
                {
                    Year++;
                    Month = 1;
                }
            }
        }
    }
}
