// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System.IO;
using System.Xml;
using UnityEditor;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace OpenXR.Extensions
{
    public class AndroidManifestHelper
    {
        public static void AddOrRemoveTag(XmlDocument doc, string @namespace, string path, string elementName, string name,
            bool required, bool modifyIfFound, params string[] attrs) // name, value pairs
        {
            var nodes = doc.SelectNodes(path + "/" + elementName);
            XmlElement element = null;
            foreach (XmlElement e in nodes)
            {
                if (name == null || name == e.GetAttribute("name", @namespace))
                {
                    element = e;
                    break;
                }
            }

            if (required)
            {
                if (element == null)
                {
                    var parent = doc.SelectSingleNode(path);
                    element = doc.CreateElement(elementName);
                    element.SetAttribute("name", @namespace, name);
                    parent.AppendChild(element);
                }

                for (int i = 0; i < attrs.Length; i += 2)
                {
                    if (modifyIfFound || string.IsNullOrEmpty(element.GetAttribute(attrs[i], @namespace)))
                    {
                        if (attrs[i + 1] != null)
                        {
                            element.SetAttribute(attrs[i], @namespace, attrs[i + 1]);
                        }
                        else
                        {
                            element.RemoveAttribute(attrs[i], @namespace);
                        }
                    }
                }
            }
            else
            {
                if (element != null && modifyIfFound)
                {
                    element.ParentNode.RemoveChild(element);
                }
            }
        }

    }

}
