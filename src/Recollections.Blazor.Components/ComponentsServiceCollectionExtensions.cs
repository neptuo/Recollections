using Microsoft.Extensions.DependencyInjection.Extensions;
using Neptuo;
using Neptuo.Identifiers;
using Neptuo.Recollections;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Components.Editors;
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
            services.AddTransient<ModalInterop>();
            services.AddSingleton<FileUploader>();
            services.AddTransient<FileUploadInterop>();
            services.AddTransient<InlineMarkdownEditInterop>();
            services.AddTransient<InlineTextEditInterop>();
            services.AddTransient<MarkdownConverter>();
            services.AddTransient<Downloader>();
            services.AddTransient<MapInterop>();
            services.AddTransient<TooltipInterop>();
            services.AddTransient<DropdownInterop>();
            services.AddTransient<PopoverInterop>();
            services.AddTransient<ElementReferenceInterop>();
            services.AddTransient<WindowInterop>();
            services.AddSingleton<FreeLimitsNotifier>();
            services.AddTransient<DocumentTitleInterop>();
            services.AddTransient<GalleryInterop>();
            services.AddTransient<AutoloadNextInterop>();
            services.AddTransient<ImageInterop>();
            services.AddTransient<OffcanvasInterop>();
            services.AddTransient<IFreeLimitsNotifier, FreeLimitsNotifier>(provider => provider.GetRequiredService<FreeLimitsNotifier>());
            services.AddSingleton<TemplateService>();
            services.AddTransient<ThemeInterop>();
            services.AddSingleton<ExceptionPanelSuppression>();

            return services;
        }
    }
}
