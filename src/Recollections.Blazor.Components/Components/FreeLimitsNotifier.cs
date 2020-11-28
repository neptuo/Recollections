using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    internal class FreeLimitsNotifier : IFreeLimitsNotifier
    {
        public event Action OnShow;

        public void Show() => OnShow?.Invoke();
    }
}
