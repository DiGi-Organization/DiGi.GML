using DiGi.GML.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DiGi.GML
{
    public static partial class Query
    {
        /// <summary>
        /// Streams the feature members of a GML feature collection file one at a time, without loading the document.
        /// <para>Unlike <see cref="Convert.ToGML{T}(string?)"/>, which reads the whole file into a string and then into an <see cref="XmlDocument"/> (a file of 0.7 - 1.6 GB fails with <see cref="System.OutOfMemoryException"/>), only one <c>gml:featureMember</c> is held as XML at a time. Elements no assembly models are skipped.</para>
        /// </summary>
        /// <param name="path">The path to the GML file.</param>
        /// <returns>The feature members in document order; empty when the path is empty, the file does not exist or its root is not a <c>FeatureCollection</c>.</returns>
        public static IEnumerable<IFeatureMember> FeatureMembers(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                yield break;
            }

            XmlReaderSettings xmlReaderSettings = new()
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            using XmlReader xmlReader = XmlReader.Create(path, xmlReaderSettings);

            if (xmlReader.MoveToContent() != XmlNodeType.Element || xmlReader.LocalName != "FeatureCollection")
            {
                yield break;
            }

            int depth = xmlReader.Depth;

            xmlReader.Read();
            while (!xmlReader.EOF && xmlReader.Depth > depth)
            {
                if (xmlReader.NodeType != XmlNodeType.Element || xmlReader.LocalName != "featureMember" || xmlReader.Depth != depth + 1)
                {
                    xmlReader.Skip();
                    continue;
                }

                // ReadNode advances the reader past the element.
                XmlDocument xmlDocument = new();
                XmlNode? xmlNode = xmlDocument.ReadNode(xmlReader);
                if (xmlNode is null)
                {
                    continue;
                }

                foreach (XmlNode xmlNode_Item in xmlNode.ChildNodes)
                {
                    if (xmlNode_Item is not XmlElement)
                    {
                        continue;
                    }

                    if (Create.AbstractGML(xmlNode_Item) is IFeatureMember featureMember)
                    {
                        yield return featureMember;
                    }
                }
            }
        }
    }
}
