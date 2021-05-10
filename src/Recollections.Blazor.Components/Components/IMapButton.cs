using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public interface IMapButton
    {
        public string IconIdentifier { get; }
        public string Title { get; }

        Task InitializeAsync();
        Task OnClickAsync();
    }

    public interface IMapToggleButton : IMapButton
    {
        bool IsEnabled { get; }
    }
}
