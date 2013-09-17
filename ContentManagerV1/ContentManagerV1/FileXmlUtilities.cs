using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ContentManagerV1
{
    class FileXmlUtilities
    {
        static public XDocument GenerateEmptyFileInfoDocument()
        {
            XElement rootElement = new XElement("FileInfo");
            XDeclaration declaration = new XDeclaration("1.0", "utf-8", "yes");
            XDocument doc = new XDocument(declaration, rootElement);
            return doc;
        }



        static public void AddFileInfoElement(XDocument xmlDoc, string file, string hashValue)
        {
            XElement rootElement = xmlDoc.Root;

            // first check if matching element already exists, only add if not.

            XElement newElement = new XElement("NodeInfo");
            newElement.Add(new XAttribute("Fullpath", file));
            newElement.Add(new XAttribute("Hash", hashValue));

            foreach (XElement element in rootElement.Elements())
            {
                if (XNode.DeepEquals(element, newElement))
                {
                    return;
                }
            }

            rootElement.Add(newElement);
        }

        // TODO: sort so everything is in same order every time, but let's see if that occurs naturally for now...
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




        //static public void AddDirToDirInfo(XDocument xmlDoc, string subDirPath, string hashValue)
        //{
        //    XElement rootElement = xmlDoc.Root;

        //    // first check if matching element already exists, only add if not.

        //    string dirName = System.IO.Path.GetDirectoryName(subDirPath);
        //    XElement newElement = new XElement("Subdirectory");
        //    newElement.Add(new XAttribute("directoryName", dirName));
        //    newElement.Add(new XAttribute("Hash", hashValue));

        //    foreach (XElement element in rootElement.Elements())
        //    {
        //        if (XNode.DeepEquals(element, newElement))
        //        {
        //            return;
        //        }
        //    }

        //    rootElement.Add(newElement);
        //}

        //static public void AddFileToDirInfo(XDocument xmlDoc, string filePath, string hashValue)
        //{
        //    XElement rootElement = xmlDoc.Root;

        //    // first check if matching element already exists, only add if not.

        //    string filename = System.IO.Path.GetFileName(filePath);
        //    XElement newElement = new XElement("File");
        //    newElement.Add(new XAttribute("filename", filename));
        //    newElement.Add(new XAttribute("Hash", hashValue));

        //    foreach (XElement element in rootElement.Elements())
        //    {
        //        if (XNode.DeepEquals(element, newElement))
        //        {
        //            return;
        //        }
        //    }

        //    rootElement.Add(newElement);
        //}
    }
}
