using Microsoft.Extensions.DependencyInjection.Extensions;
using Neptuo;
using Neptuo.Identifiers;
using Neptuo.Recollections.Components;
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
            services.AddSingleton<ModalNative>();
            services.AddSingleton<FileUploadInterop>();
            services.TryAdd(ServiceDescriptor.Singleton<IUniqueNameProvider, GuidUniqueNameProvider>());

            return services;
        }
    }
}
