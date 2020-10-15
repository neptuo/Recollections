using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/entries/version")]
    public class VersionController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        public ActionResult<VersionModel> Get() => Ok(new VersionModel(typeof(VersionController).Assembly.GetName().Version));
    }
}
