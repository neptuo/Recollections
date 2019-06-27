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
        public string Convert(string markdown) => CommonMarkConverter.Convert(markdown);
    }
}
