using Neptuo.Recollections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class UiOptions
    {
        public string DateFormat { get; set; }
        public string ShortDateFormat { get; set; }
        public string TimeFormat { get; set; }
        public string NumberFormat { get; set; }
        public string NumberGroupSeparator { get; set; }

        public int TextPreviewLength { get; set; }

        public string FormatWholeNumber(double value)
        {
            var numberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            numberFormat.NumberGroupSeparator = NumberGroupSeparator;
            return Math.Round(value).ToString(NumberFormat, numberFormat);
        }
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
            uiOptions.ShortDateFormat = "dd.MM.";
            uiOptions.TimeFormat = "HH:mm";
            uiOptions.NumberFormat = "#,##0";
            uiOptions.NumberGroupSeparator = " ";
            uiOptions.TextPreviewLength = 300;

            return services.AddSingleton(uiOptions);
        }
    }
}
