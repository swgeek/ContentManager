using System;
using System.IO;
using System.Xml.Linq;

namespace ContentManagerCore
{
    // xml utility method. Content Manager specific.
    public class CMXmlUtilities
    {
        static public XDocument GenerateEmptyFileInfoDocument(long filesize)
        {
            XElement rootElement = new XElement("FileInfo");
            rootElement.Add(new XAttribute("Filesize", filesize.ToString()));
            rootElement.Add(new XAttribute("Status", "todo"));
            XDeclaration declaration = new XDeclaration("1.0", "utf-8", "yes");
            XDocument doc = new XDocument(declaration, rootElement);
            return doc;
        }

        static public void AddFileInfoElement(XDocument xmlDoc, string file)
        {
            XElement rootElement = xmlDoc.Root;
            XElement newElement = new XElement("NodeInfo");
            newElement.Add(new XAttribute("Fullpath", file));

            foreach (XElement element in rootElement.Elements())
            {
                // first check if matching element already exists, only add if not.
                if (XNode.DeepEquals(element, newElement))
                {
                    return;
                }
            }

            rootElement.Add(newElement);
        }

        // TODO: sort so everything is in same order every time, but let's see if that 
        // occurs naturally for now...
        // need to do that so hash of directory contents is consistent, will use
        // to find duplicate directories in future versions
        static public XDocument GenerateDirInfoDocument(string dirPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            
            if (! dirInfo.Exists)
                throw new Exception(dirPath + " does not exist!");

            XElement rootElement = new XElement("DirInfo");

            XDeclaration declaration = new XDeclaration("1.0", "utf-8", "yes");
            XDocument doc = new XDocument(declaration, rootElement);

            DirectoryInfo[] directories =  dirInfo.GetDirectories();
            foreach (DirectoryInfo d in directories)
            {
                XElement newElement = new XElement("Subdirectory");
                newElement.Add(new XAttribute("directoryName", d.Name));
                rootElement.Add(newElement);
            }

            FileInfo[] files = dirInfo.GetFiles();
            foreach (FileInfo f in files)
            {
                XElement newElement = new XElement("File");
                newElement.Add(new XAttribute("filename", f.Name));
                rootElement.Add(newElement);
            }

            return doc;
        }

        static public XDocument GenerateRootDirInfoDocument(string dirPath)
        {
            XElement rootElement = new XElement("RootDir");
            rootElement.Add(new XAttribute("path", dirPath));
            XDeclaration declaration = new XDeclaration("1.0", "utf-8", "yes");
            XDocument doc = new XDocument(declaration, rootElement);
            return doc;
        }

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
