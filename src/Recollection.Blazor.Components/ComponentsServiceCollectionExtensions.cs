using Neptuo;
using Neptuo.Recollection.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ComponentsServiceCollectionExtensions
    {
        public static IServiceCollection AddComponents(this IServiceCollection services)
        {
            Ensure.NotNull(services, "services");
            return services.AddSingleton<ModalNative>();
        }
    }
}
