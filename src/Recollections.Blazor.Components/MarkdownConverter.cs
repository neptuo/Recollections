using CommonMark;
using CommonMark.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Neptuo.Recollections;

public class MarkdownConverter
{
    private static readonly CommonMarkSettings settings;

    static MarkdownConverter()
    {
        settings = CommonMarkSettings.Default.Clone();
        settings.RenderSoftLineBreaksAsLineBreaks = true;
        settings.AdditionalFeatures |= CommonMarkAdditionalFeatures.StrikethroughTilde;
        settings.OutputDelegate = (doc, output, settings) => new CustomHtmlFormatter(output, settings).WriteDocument(doc);
    }

    public string Convert(string markdown) => CommonMarkConverter.Convert(markdown, settings);
}

class CustomHtmlFormatter : CommonMark.Formatters.HtmlFormatter
{
    public CustomHtmlFormatter(TextWriter target, CommonMarkSettings settings)
        : base(target, settings)
    {
    }

    private Regex urlRegex = new Regex(@"(?<Protocol>\w+):\/\/(?<Domain>[\w@][\w.:@]+)\/?[\w\.?=%&=\-@/$,]*");

    protected override void WriteInline(Inline inline, bool isOpening, bool isClosing, out bool ignoreChildNodes)
    {
        if (inline.Tag == InlineTag.String)
        {
            Match match = urlRegex.Match(inline.LiteralContent);
            if (match.Success)
            {
                var lastIndex = 0;
                while (match.Success)
                {
                    Write(inline.LiteralContent.Substring(lastIndex, match.Index));
                    Write($"<a href='");
                    WriteEncodedUrl(match.Value);
                    Write($"' target='_blank'>{match.Value}</a>");

                    lastIndex = match.Index + match.Length;

                    match = match.NextMatch();
                }

                if (lastIndex < inline.LiteralContent.Length)
                    Write(inline.LiteralContent.Substring(lastIndex, inline.LiteralContent.Length - lastIndex));

                ignoreChildNodes = false;
            }
            else
            {
                base.WriteInline(inline, isOpening, isClosing, out ignoreChildNodes);
            }
        }
        else
        {
            base.WriteInline(inline, isOpening, isClosing, out ignoreChildNodes);
        }
    }
}
