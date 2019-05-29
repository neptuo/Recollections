using Microsoft.AspNetCore.Mvc;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries.Controllers
{
    [Route("api/entries/[action]")]
    public class TimelineController : ControllerBase
    {
        private readonly DataContext dataContext;

        public TimelineController(DataContext dataContext)
        {
            Ensure.NotNull(dataContext, "dataContext");
            this.dataContext = dataContext;
        }


    }
}
