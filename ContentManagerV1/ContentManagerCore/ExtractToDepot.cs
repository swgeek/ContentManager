using MpvUtilities;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ContentManagerCore
{
    public class ExtractFromDepot
    {
            static public void ExtractFilesAndDirs(string sourceDir, string destDir)
            {
                string rootDirXmlFile = System.IO.Path.Combine(sourceDir, "working", "RootDir.xml");
                XDocument RootDirXmlDoc = XDocument.Load(rootDirXmlFile);
                string rootDir = RootDirXmlDoc.Root.Attribute("path").Value.ToString();
                string dirNameOnly = System.IO.Path.GetFileName(rootDir.TrimEnd(System.IO.Path.DirectorySeparatorChar));
                string newPath = System.IO.Path.Combine(destDir, dirNameOnly);
                RecursivelyRestoreFiles(sourceDir, rootDir, newPath);
            }

            static public void RecursivelyRestoreFiles(string depotRootDir, string originalDirPath, string newDirPath)
            {
                Directory.CreateDirectory(newDirPath);

                string hashValue = MpvUtilities.SH1HashUtilities.HashString(originalDirPath);

                string dirXmlPath = DepotPathUtilities.GetExistingXmlDirectoryInfoFileName(originalDirPath, depotRootDir);
                    
                XDocument dirXmlDoc = XDocument.Load(dirXmlPath);

                XElement[] fileElements = dirXmlDoc.Root.Descendants("File").ToArray();

                foreach (XElement element in fileElements)
                {
                    string fileHash = element.Attribute("Hash").Value.ToString();
                    string fileName = element.Attribute("filename").Value.ToString();
                    string filePath = DepotPathUtilities.GetHashFilePath(depotRootDir, fileHash);
                    string newFilePath = Path.Combine(newDirPath, fileName);
                    File.Copy(filePath, newFilePath);
                }

                XElement[] dirElements = dirXmlDoc.Root.Descendants("Subdirectory").ToArray();
                foreach (XElement element in dirElements)
                {
                    string subdir = element.Attribute("directoryName").Value.ToString();
                    RecursivelyRestoreFiles(depotRootDir, Path.Combine(originalDirPath, subdir), Path.Combine(newDirPath, subdir));
                }
            }

    }
}
