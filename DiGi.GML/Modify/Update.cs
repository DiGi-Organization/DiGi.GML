using DiGi.GML.Classes;
using DiGi.GML.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace DiGi.GML
{
    public static partial class Modify
    {
        /// <summary>
        /// Updates the properties of an <see cref="IAbstractGML"/> instance based on the values provided in an <see cref="XmlNode"/>.
        /// <para>A property element that is empty in every occurrence and carries an <c>href</c> attribute in the XLink namespace binds by reference: a <c>string</c> property receives the first href verbatim, a list of strings one href per occurrence in document order. References are stored unresolved; resolving them is the consumer's job. The match is on the XLink namespace URI, never on the prefix, and empty elements without an XLink <c>href</c> are skipped.</para>
        /// </summary>
        /// <param name="abstractGML">The GML object to be updated.</param>
        /// <param name="xmlNode">The XML node containing the source data for the update.</param>
        /// <returns>True if at least one property was successfully updated; otherwise, false.</returns>
        public static bool Update(this IAbstractGML abstractGML, XmlNode xmlNode)
        {
            if (xmlNode == null || abstractGML == null)
            {
                return false;
            }

            Dictionary<string, List<XmlNode>> dictionary_XmlNodes = [];
            if (xmlNode.HasChildNodes)
            {
                foreach (XmlNode xmlNode_Child in xmlNode.ChildNodes)
                {
                    string? name = xmlNode_Child?.LocalName;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!dictionary_XmlNodes.TryGetValue(name!, out List<XmlNode> xmlNodes) || xmlNodes == null)
                    {
                        xmlNodes = [];
                        dictionary_XmlNodes[name!] = xmlNodes;
                    }

                    xmlNodes.Add(xmlNode_Child!);
                }
            }

            Dictionary<string, XmlAttribute> dictionary_XmlAttribute = [];
            if (xmlNode.Attributes != null)
            {
                foreach (XmlAttribute xmlAttribute in xmlNode.Attributes)
                {
                    string? name = xmlAttribute?.LocalName;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    dictionary_XmlAttribute[name!] = xmlAttribute!;
                }
            }

            List<PropertyInfo>? propertyInfos = Query.PropertyInfos(abstractGML);
            if (propertyInfos == null || propertyInfos.Count == 0)
            {
                return false;
            }

            bool result = false;
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (dictionary_XmlAttribute.TryGetValue(propertyInfo.Name, out XmlAttribute xmlAttribute) && xmlAttribute != null)
                {
                    if (!Query.TryConvert(xmlAttribute.Value, propertyInfo, out object? value))
                    {
                        continue;
                    }

                    propertyInfo.SetValue(abstractGML, value);
                    result = true;
                    continue;
                }

                if (dictionary_XmlNodes.TryGetValue(propertyInfo.Name, out List<XmlNode> xmlNodes) && xmlNodes != null && xmlNodes.Count != 0)
                {
                    Type type_Property = propertyInfo.PropertyType;
                    Type type_Property_Temp = Nullable.GetUnderlyingType(type_Property);
                    if (type_Property_Temp != null)
                    {
                        type_Property = type_Property_Temp;
                    }

                    bool isString = type_Property == typeof(string);
                    bool isStringList = type_Property == typeof(IList<string>)
                        || (typeof(IList).IsAssignableFrom(type_Property) && type_Property.IsGenericType && type_Property.GenericTypeArguments.Length == 1 && type_Property.GenericTypeArguments[0] == typeof(string));

                    if (isString || isStringList)
                    {
                        List<string>? hrefs = XlinkHrefs(xmlNodes);
                        if (hrefs != null && hrefs.Count != 0)
                        {
                            if (isString)
                            {
                                propertyInfo.SetValue(abstractGML, hrefs[0]);
                            }
                            else
                            {
                                propertyInfo.SetValue(abstractGML, hrefs);
                            }

                            result = true;
                            continue;
                        }
                    }

                    if (!xmlNodes.TrueForAll(x => x.HasChildNodes))
                    {
                        continue;
                    }

                    object? value = null;

                    List<XmlNode> xmlNodes_Temp = xmlNodes.ConvertAll(x => x.ChildNodes[0]);
                    if (xmlNodes_Temp.TrueForAll(x => x is XmlText))
                    {
                        string text = string.Join("\t", xmlNodes.ConvertAll(x => x.InnerText));

                        if (!Query.TryConvert(text, propertyInfo, out value))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        Type type = propertyInfo.PropertyType;
                        Type type_Temp = Nullable.GetUnderlyingType(type);
                        if (type_Temp != null)
                        {
                            type = type_Temp;
                        }

                        if (typeof(IAbstractGML).IsAssignableFrom(type))
                        {
                            if (xmlNodes.Count == 1)
                            {
                                value = Create.AbstractGML(xmlNodes_Temp[0]);
                            }
                        }
                        else if (typeof(IList).IsAssignableFrom(type))
                        {
                            if (Activator.CreateInstance(type) is IList list)
                            {
                                // A wrapper may hold several items (gml:segments) or repeat once per item (gml:interior, gml:featureMember)
                                foreach (XmlNode xmlNode_Wrapper in xmlNodes)
                                {
                                    foreach (XmlNode xmlNode_Item in xmlNode_Wrapper.ChildNodes)
                                    {
                                        if (xmlNode_Item is not XmlElement)
                                        {
                                            continue;
                                        }

                                        IAbstractGML? abstractGML_Item = Create.AbstractGML(xmlNode_Item);
                                        if (abstractGML_Item == null)
                                        {
                                            continue;
                                        }

                                        list.Add(abstractGML_Item);
                                    }
                                }

                                value = list;
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }

                    propertyInfo.SetValue(abstractGML, value);
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Updates the properties of a destination GML object using values from a source GML object of the same type.
        /// </summary>
        /// <typeparam name="T">A type that implements <see cref="IAbstractGML"/>.</typeparam>
        /// <param name="abstractGML_Source">The source GML object containing the data to copy.</param>
        /// <param name="abstractGML_Destination">The destination GML object to be updated.</param>
        /// <returns>True if the update process completed; otherwise, false.</returns>
        public static bool Update<T>(this T abstractGML_Source, T abstractGML_Destination) where T : IAbstractGML
        {
            if (abstractGML_Source == null || abstractGML_Destination == null)
            {
                return false;
            }

            List<PropertyInfo>? propertyInfos = abstractGML_Destination.PropertyInfos();
            if (propertyInfos == null || propertyInfos.Count == 0)
            {
                return true;
            }

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                Update(abstractGML_Source, abstractGML_Destination, propertyInfo);
            }

            return true;
        }

        /// <summary>
        /// Updates a specific property of a destination GML object using the value from the corresponding property of a source GML object.
        /// </summary>
        /// <typeparam name="T">A type that implements <see cref="IAbstractGML"/>.</typeparam>
        /// <param name="abstractGML_Source">The source GML object containing the data to copy.</param>
        /// <param name="abstractGML_Destination">The destination GML object to be updated.</param>
        /// <param name="propertyInfo">The property information of the member to be updated.</param>
        /// <returns>True if the property was successfully updated; otherwise, false.</returns>
        public static bool Update<T>(this T abstractGML_Source, T abstractGML_Destination, PropertyInfo propertyInfo)
        {
            if (abstractGML_Source == null || abstractGML_Source == null)
            {
                return false;
            }

            object? value = propertyInfo.GetValue(abstractGML_Source);

            if (value is IAbstractGML)
            {
                value = ((AbstractGML)value).Clone();
            }
            else if (value is string)
            {
            }
            else if (value is IList)
            {
                if (Activator.CreateInstance(value.GetType()) is not IList list)
                {
                    return false;
                }

                foreach (object @object in (IEnumerable)value)
                {
                    if (@object is IAbstractGML)
                    {
                        list.Add(((AbstractGML)@object).Clone());
                    }
                    else
                    {
                        list.Add(@object);
                    }
                }
                value = list;
            }

            propertyInfo.SetValue(abstractGML_Destination, value);
            return true;
        }

        private const string XLinkNamespace = "http://www.w3.org/1999/xlink";

        private static List<string>? XlinkHrefs(List<XmlNode> xmlNodes)
        {
            List<string> hrefs = [];
            foreach (XmlNode xmlNode in xmlNodes)
            {
                if (xmlNode.HasChildNodes || xmlNode.Attributes == null)
                {
                    return null;
                }

                string? href = null;
                foreach (XmlAttribute xmlAttribute in xmlNode.Attributes)
                {
                    if (string.Equals(xmlAttribute.LocalName, "href", StringComparison.Ordinal) && string.Equals(xmlAttribute.NamespaceURI, XLinkNamespace, StringComparison.Ordinal))
                    {
                        href = xmlAttribute.Value;
                        break;
                    }
                }

                if (href == null)
                {
                    return null;
                }

                hrefs.Add(href);
            }

            return hrefs;
        }
    }
}
