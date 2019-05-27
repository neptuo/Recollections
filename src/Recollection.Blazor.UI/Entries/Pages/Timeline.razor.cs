using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries.Pages
{
    public class TimelineModel : ComponentBase
    {
        public List<TimelineEntryModel> Entries { get; } = new List<TimelineEntryModel>();

        protected async override Task OnInitAsync()
        {
            await base.OnInitAsync();

            Entries.Add(new TimelineEntryModel()
            {
                Title = "Tenerife 2018",
                When = new DateTime(2018, 9, 27),
                Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam nec odio dictum lorem aliquet dignissim eu a ex. Integer ac fermentum risus. Maecenas consequat dolor non eleifend interdum. Quisque nec pretium felis. Proin vitae vestibulum purus, sit amet hendrerit nisl. Vivamus at elementum tellus. Duis non urna ut sapien dignissim feugiat. Nullam laoreet libero eget dolor auctor luctus. Mauris elit massa, elementum eget aliquet ac, molestie porta sapien. Suspendisse metus justo, feugiat ut dignissim vel, maximus nec ex. Nulla sagittis urna vitae ornare dictum."
            });
        }
    }
}
