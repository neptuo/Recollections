using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public class InlineGpsEditModel : InlineEditModel<LocationModel>
    {
        protected string FormatToString()
        {
            if (Value == null)
                return null;

            string result = $"{Value.Latitude}, {Value.Longitude}";
            if (Value.Altitude != null)
                result += $" ({Value.Altitude})";

            return result;
        }
    }
}
