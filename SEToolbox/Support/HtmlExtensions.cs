using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using SharpDX.DXGI;


namespace SEToolbox.Support
{


    internal static class HtmlExtensions
    {
        private static readonly char[] _htmlChars = ['<', '>', '&', '"'];

        #region BeginDocument

        internal static void BeginDocument(this StringWriter writer, string title, string inlineStyleSheet)
        {
            writer.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html;charset=UTF-8\" />");
            writer.WriteLine("<html>");
            writer.WriteLine("<style>");
            writer.WriteLine(inlineStyleSheet);
            writer.WriteLine("</style>");
            writer.WriteLine("<head>");
            writer.WriteLine("<title>" + title + "</title>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body>");
        }

        #endregion

        #region EndDocument

        internal static void EndDocument(this StringWriter writer)
        {
            writer.Write("</body>");
            writer.Write("</html>");
        }

        #endregion

        #region RenderElement


        internal static void RenderElement(this StringWriter writer, string text, string tagName = null)
        {
            writer.Write("<" + tagName + ">" + text + "</" + tagName + ">");
        }

        internal static void RenderElement(this StringWriter writer, string tagName = null)
        {
            writer.Write("<" + tagName + "/>");
        }
        internal static void RenderElement(this StringWriter writer, bool start, string tagName, string format, params object[] args)
        {
            writer.Write((start ? "<" : "</") + string.Format(format, tagName, args) + ">");
        }

        internal static void RenderElement(this StringWriter writer, string tagName, object value, string text = null, string format = null, params object[] arg)
        {
            Type type = value?.GetType();
            writer.Write(type?.Name switch
            {
                null => string.Empty,
                _ when type.IsInstanceOfType(value) => Convert.ToString(value),
                _ when type.IsInstanceOfType(text) => text,
                _ when type.IsInstanceOfType(format) => string.Format(format, arg),
                _ => throw new ArgumentException($"Unsupported type: {type.Name}"),
            });
            writer.RenderElement(tagName, type);
        }

        internal static void RenderElement(this StringWriter writer, string tagName, string text, string format, params object[] arg)
        {
            RenderTagStart(writer, tagName);
            if (text != null)
            {
                var index = text.IndexOfAny(_htmlChars);
                if (index >= 0)
                {
                    writer.Write(text.Substring(0, index));
                    writer.Write(HtmlEncode(text.Substring(index)));
                }
                else
                {
                    writer.Write(text);
                }
            }
            if (format != null)
                writer.Write(string.Format(format, arg));
            RenderTagEnd(writer, tagName);
        }

        internal static void AddAttribute(this StringWriter writer, string attributeName, string attributeValue)
        => writer.Write($"{attributeName}=\"{attributeValue}\"");


        private static string HtmlEncode(string text)
        {
            int index = text.IndexOfAny(_htmlChars);
            if (index < 0)
                return text;

            var result = new StringBuilder(text.Length + 6);
            int textLength = text.Length;
            for (int i = 0; i < textLength; ++i)
            {

                result.Append(text[i] switch
                {
                    '<' => "&lt;",
                    '>' => "&gt;",
                    '&' => "&amp;",
                    '"' => "&quot;",
                    _ => $"{text[i]}",
                });
            }

            return result.ToString();
        }

       // internal static void RenderTag(this StringWriter writer, string tagName, bool start = true)
       // {
       //       writer.Write((start ? "<" : "</") + tagName + ">");
        //}
        internal static void RenderTagStart(this StringWriter writer, string tagName)
        {
            writer.Write("<" + tagName + ">");
        }
          internal static void RenderTagEnd(this StringWriter writer, string tagName)
        {
            writer.Write("</" + tagName + ">");
        }
        


        #endregion

        #region BeginTable

        internal static void BeginTable(this StringWriter writer, string border, string cellpadding, string cellspacing, string[] headings)
        {
            writer.Write("<table");
            string str = null;
            writer.Write(!string.IsNullOrEmpty(str) switch
            {
                bool when str == border => $" border=\"{border}\"",
                bool when str == cellpadding => $" cellpadding=\"{cellpadding}\"",
                bool when str == cellspacing => $" cellspacing=\"{cellspacing}\"",
                _ => string.Empty
            });
            writer.Write(">");
            writer.WriteLine("<thead><tr>");

            foreach (string header in headings)
            {
                writer.WriteLine($"<th>{header}</th>");
            }

            writer.Write("</tr>");
            writer.Write("</thead>");
        }

        #endregion

        #region EndTable

        internal static void EndTable(this StringWriter writer)
        {
            writer.Write("</table>");
}
    }
    #endregion

    internal static class HtmlWriter
    {
        #region HtmlWriter
        internal class HtmlWriterSettings
        {
            public HtmlWriterSettings()
            {
                Encoding = Encoding.UTF8;
            }

            public Encoding Encoding { get; set; }
            public string Doctype { get; set; }
            public string DoctypeVersion { get; set; }
            public string DoctypePublic { get; set; }
            public string DoctypeSystem { get; set; }
        }
        public class HtmlElement
        {
            public string Tag { get; set; }
            public string Text { get; set; }
        }

        internal static void WriteHtml(
            this StringWriter writer,
            string title,
            string inlineStyleSheet,
            (string tag, string text)[] elements,
            string border,
            string cellpadding,
            string cellspacing,
            string[] headings,
            string[][] rows)
        {
            // Start doc
            writer.BeginDocument(title, inlineStyleSheet);

            // Render elements
            if (elements != null)
            {
                foreach (var (tag, text) in elements)
                {
                    writer.RenderElement(tag, text);
                }
            }

            // Render table
            if (headings != null && headings.Length > 0)
            {
                writer.BeginTable(border, cellpadding, cellspacing, headings);

                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        writer.Write("<tr>");
                        foreach (var cell in row)
                        {
                            writer.RenderElement("td", cell ?? string.Empty);
                        }
                        writer.WriteLine("</tr>");
                    }
                }

                writer.EndTable();
            }

            // End doc
            writer.EndDocument();
        }

        #endregion
    }
}
