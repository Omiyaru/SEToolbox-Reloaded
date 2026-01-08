using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml;
using System.Xml.XPath;

namespace SEToolbox.Support
{
    internal static class XmlExtension
    {
        #region BuildXmlNamespaceManager

        internal static XmlNamespaceManager BuildXmlNamespaceManager(this XmlDocument document)
        {
            XPathNavigator nav = document.CreateNavigator();
            XmlNamespaceManager manager = new(nav.NameTable);

            // Fetch out the namespace from the file. This is hacky approach.
            MatchCollection matches = Regex.Matches(document.InnerXml, @"(?:\bxmlns:?(?<schema>[^=]*)=[""](?<key>[^""]*)""[\s>])");
            foreach (Match match in matches)
            {
                string schemaName = match.Groups["schema"].Value;
                Action action = string.IsNullOrEmpty(schemaName) ? () => manager.AddNamespace("", match.Groups["key"].Value) :
                                                                   () => manager.AddNamespace(schemaName, match.Groups["key"].Value);
                action();
            }
            
            return manager;
        }

        #endregion

        #region GetXMLObject

        /// <summary>
        /// Helper extension method to load string data from XML node values into native types that aren't necessarily serializable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="navRoot"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static T ToValue<T>(this XPathNavigator navRoot, string name)
        {
            XPathNavigator node = navRoot.SelectSingleNode(name) ?? throw new ArgumentNullException(nameof(name), "Node cannot be null.");
            object item = node?.Value;

            return typeof(T) switch
            {
                Type t when t == typeof(string) => (T)item,
                Type t when t == typeof(int) => (T)(object)Convert.ToInt32(item),
                Type t when t == typeof(long) => (T)(object)Convert.ToInt64(item),
                Type t when t == typeof(IntPtr) => (T)(object)new IntPtr(Convert.ToInt64(item)),
                Type t when t == typeof(double) => (T)(object)Convert.ToDouble(item, CultureInfo.InvariantCulture),
                Type t when t == typeof(DateTime) => (T)(object)DateTime.Parse((string)item, null),
                Type t when t == typeof(DateTimeOffset) => (T)(object)DateTimeOffset.Parse((string)item, null),
                Type t when t == typeof(bool) => (T)(object)ConvertToBoolean(item),
                Type t when t == typeof(Guid) => (T)(object)new Guid((string)item),
                Type t when t.BaseType == typeof(Enum) => (T)Enum.Parse(typeof(T), (string)item),
                Type t when t == typeof(CultureInfo) => (T)(object)CultureInfo.GetCultureInfoByIetfLanguageTag((string)item),
                Type t when t == typeof(Point3D) => (T)(object)new Point3D(
                    double.Parse(node.SelectSingleNode("X")?.Value, CultureInfo.InvariantCulture),
                    double.Parse(node.SelectSingleNode("Y")?.Value, CultureInfo.InvariantCulture),
                    double.Parse(node.SelectSingleNode("Z")?.Value, CultureInfo.InvariantCulture)),
                Type t when t == typeof(Rect) => (T)new RectConverter().ConvertFromString((string)item),
                Type t when t == typeof(XmlDocument) => (T)item,
                _ => throw new NotImplementedException($"The datatype [{typeof(T).Name}] has not been catered for.")
            };
        }

        private static bool ConvertToBoolean(object item)
        {
            return item is string str && int.TryParse(str, out int result)
                ? Convert.ToBoolean(result)
                : Convert.ToBoolean(item);
        }

        #endregion

        #region WriteElementFormat

        internal static void WriteElementFormat(this XmlWriter writer, string localName, string format, params object[] arg)
        {
            writer.WriteElementString(localName, string.Format(format, arg));
        }

        #endregion

        #region WriteAttributeFormat

        internal static void WriteAttributeFormat(this XmlWriter writer, string localName, string format, params object[] arg)
        {
            writer.WriteAttributeString(localName, string.Format(format, arg));
        }

        #endregion
    }
}
