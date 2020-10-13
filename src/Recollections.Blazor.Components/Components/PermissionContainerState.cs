using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class PermissionContainerState
    {
        public FormState FormState { get; } = new FormState();

        public bool IsOwner { get; set; }
        public bool IsEditable { get => FormState.IsEditable; set => FormState.IsEditable = value; }
        public bool IsReadable { get; set; }

        public PermissionContainerState()
        {
            IsOwner = true;
            IsEditable = true;
            IsReadable = true;
        }
    }
}
