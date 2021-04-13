using CommonMark;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class MarkdownConverter
    {
        private static readonly CommonMarkSettings settings;

        static MarkdownConverter()
        {
            settings = CommonMarkSettings.Default.Clone();
            settings.RenderSoftLineBreaksAsLineBreaks = true;
        }

        public string Convert(string markdown) => CommonMarkConverter.Convert(markdown, settings);
    }
}
