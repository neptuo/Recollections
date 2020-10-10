using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class ShareButton
    {
        [Parameter]
        public string EntryId { get; set; }

        protected Modal Modal { get; set; }

        protected void OnToggle()
            => Modal.Show();
    }
}
