using Neptuo.Recollection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection
{
    public class UiOptions
    {
        public string DateFormat { get; set; }
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

            return services.AddSingleton(uiOptions);
        }
    }
}