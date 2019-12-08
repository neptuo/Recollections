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
    public partial class InlineGpsList : ComponentBase
    {
        [Parameter]
        public List<LocationModel> Value { get; set; }

        [Parameter]
        public Action<List<LocationModel>> ValueChanged { get; set; }

        protected void OnValueChanged(LocationModel newValue, LocationModel oldValue)
        {
            if (Value != null)
            {
                if (oldValue != null)
                    Value.Remove(oldValue);

                Value.Add(newValue);

                ValueChanged?.Invoke(Value);
            }
        }

        protected void OnItemRemoved(LocationModel item)
        {
            if (Value != null)
            {
                Value.Remove(item);
                ValueChanged?.Invoke(Value);
            }
        }
    }
}
