using Neptuo.Recollections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class UiOptions
    {
        public string DateFormat { get; set; }
        public string TimeFormat { get; set; }

        public int TextPreviewLength { get; set; }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class UiOptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddUiOptions(this IServiceCollection services)
        {
            UiOptions uiOptions = new UiOptions();
            uiOptions.DateFormat = "dd.MM.yyyy";
            uiOptions.TimeFormat = "HH:mm";
            uiOptions.TextPreviewLength = 300;

            return services.AddSingleton(uiOptions);
        }
    }
}