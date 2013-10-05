using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MpvUtilities
{
    public class MoreXmlUtilities
    {





        public static void MergeXmlFiles(string sourceXmlFile, string destinationXmlFile)
        {
            XDocument destXml;
            XDocument sourceXml;

            if (File.Exists(destinationXmlFile))
            {
                // get existing
                destXml = XDocument.Load(destinationXmlFile);
            }
            else
            {
                throw new Exception(destinationXmlFile + "does not exist!");
            }

            if (File.Exists(sourceXmlFile))
            {
                // get existing
                sourceXml = XDocument.Load(sourceXmlFile);
            }
            else
            {
                throw new Exception(sourceXmlFile + "does not exist!");
            }
 
            // merge the two
            foreach (XElement elementToAdd in sourceXml.Root.Elements())
            {
                AddElementToXml(destXml.Root, elementToAdd);
            }

            // maybe change so save only if xml changes
            destXml.Save(destinationXmlFile);

        }

        public static void AddElementToXml(XElement rootElement, XElement newElement)
        {
            foreach (XElement element in rootElement.Elements())
            {
                if (XNode.DeepEquals(element, newElement))
                {
                    return;
                }
            }

            rootElement.Add(newElement);
        }

        public static string GetRootDirectoryFromXmlRootFile(string xmlFileName)
        {
            XDocument xdoc = XDocument.Load(xmlFileName);
            string rootDir = xdoc.Root.Attribute("path").Value.ToString();
            return rootDir;
        }
    }
}
