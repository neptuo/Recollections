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

    private Regex urlRegex = new Regex(@"(?:\w+:\/\/[\w@][\w.:@]+|www\.[\w][\w.:@]+)\/?[\w\.?=%&=\-@/$,#]*");

    protected override void WriteInline(Inline inline, bool isOpening, bool isClosing, out bool ignoreChildNodes)
    {
        if (inline.Tag == InlineTag.Link)
        {
            if (isOpening)
            {
                Write("<a href=\"");
                WriteEncodedUrl(inline.TargetUrl);
                Write("\" target=\"_blank\" onclick=\"event.stopPropagation()\"");
                if (!string.IsNullOrEmpty(inline.LiteralContent))
                {
                    Write(" title=\"");
                    WriteEncodedHtml(inline.LiteralContent);
                    Write("\"");
                }
                Write(">");
            }
            if (isClosing)
            {
                Write("</a>");
            }
            ignoreChildNodes = false;
        }
        else if (inline.Tag == InlineTag.String)
        {
            Match match = urlRegex.Match(inline.LiteralContent);
            if (match.Success)
            {
                var lastIndex = 0;
                while (match.Success)
                {
                    Write(inline.LiteralContent.Substring(lastIndex, match.Index));
                    Write("<a href=\"");
                    var url = match.Value;
                    if (url.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                        Write("https://");
                    WriteEncodedUrl(url);
                    Write("\" target=\"_blank\" onclick=\"event.stopPropagation()\">");
                    WriteEncodedHtml(url);
                    Write("</a>");

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
