using MpvUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ContentManagerCore
{
    // gets a list of all files in the depot
    public class DepotFileLister
    {
        public static int WriteNewObjectFileListToFile(string depotRootPath, string filelistDbRootPath)
        {
            string depotName = System.IO.Path.GetFileName(depotRootPath);
        
            int count = 0;
            string objectDirName = DepotPathUtilities.GetObjectStoreDirNamePath(depotRootPath);
            if (!Directory.Exists(objectDirName))
                throw new Exception(objectDirName + " does not exist - did you give correct root of archive directory?");

            string currentDirectoryName = string.Empty;
            for (int i = 0x00; i < 0x100; i++)
            {
                List<string> filenameList = GetListOfObjectFilesStartingWithX2(objectDirName, i);

                if (filenameList.Count > 0)
                {
                    count += filenameList.Count;

                    string outputFileName = System.IO.Path.Combine(filelistDbRootPath, i.ToString("X2")) + ".txt";

                    if (File.Exists(outputFileName))
                    {
                        string[] existingFilenames = File.ReadAllLines(outputFileName);
                        filenameList.AddRange(existingFilenames);
                    }

                    File.WriteAllLines(outputFileName, filenameList.Distinct().ToArray());
                }
            }
            return count;
        }

        public static List<string> GetListOfObjectFilesStartingWithX2(string objectDirName, int X2)
        {
            List<string> filelist = new List<string>();
            string currentDirectoryName = System.IO.Path.Combine(objectDirName, X2.ToString("X2"));
            if (Directory.Exists(currentDirectoryName))
            {
                DirectoryInfo currentDirectory = new DirectoryInfo(currentDirectoryName);

                foreach (FileInfo file in currentDirectory.GetFiles())
                {
                    if (file.Extension != ".xml")
                    {
                        //if ((searchString != null) && (file.Name.Contains(searchString)))
                        filelist.Add(file.Name);
                    }
                }
            }  
            return filelist;
        }

        public static string[] GetListOfHashedFilesInDepot(string depotRootPath)
        {
            string depotName = System.IO.Path.GetFileName(depotRootPath);
            List<string> filelist = new List<string>();
            string objectDirName = DepotPathUtilities.GetObjectStoreDirNamePath(depotRootPath);

            if (!Directory.Exists(objectDirName))
                throw new Exception(objectDirName + " does not exist - did you give correct root of archive directory?");

            string currentDirectoryName = string.Empty;
            for (int i = 0x00; i < 0x100; i++)
            {
                List<string> subFileList = GetListOfObjectFilesStartingWithX2(objectDirName, i);
                filelist.AddRange(subFileList);
            }
            return filelist.ToArray();
        }

        public static ObjectFileInfo[] SearchForFilenamesContaining(string depotRoot, string searchString, bool sortBySize)
        {
            string depotName = DepotPathUtilities.GetDepotName(depotRoot);
            List<ObjectFileInfo> filelist = new List<ObjectFileInfo>();

            string[] filenameArray = GetListOfHashedFilesInDepot(depotRoot);
            
            foreach (string file in filenameArray)
            {
                ObjectFileInfo newNode = new ObjectFileInfo(depotRoot, file);
                if (newNode.AFilenameContains(searchString))
                {
                    filelist.Add(newNode);
                }
            }
            
            if (sortBySize)
            {
               filelist.Sort((a, b) => b.FileSize.CompareTo(a.FileSize));
                // This does not sort properly: filelist.OrderByDescending(x => x.FileSize);
            }
            return filelist.ToArray();
        }

        public static DirListing GetDirListing(string originalPath, string depotRootPath)
        {
            string dirInfoPath = DepotPathUtilities.GetExistingXmlDirectoryInfoFileName(originalPath, depotRootPath);
            XDocument dirXml = XDocument.Load(dirInfoPath);

            DirListing listing = new DirListing(originalPath);

            foreach (XElement subdirXml in dirXml.Root.Elements("Subdirectory"))
            {
                string subdirName = subdirXml.Attribute("directoryName").Value.ToString();
                string subdirPath = System.IO.Path.Combine(originalPath, subdirName);
                listing.Directories.Add(subdirName);
            }

            foreach (XElement fileXml in dirXml.Root.Elements("File"))
            {
                string filename = fileXml.Attribute("filename").Value.ToString();
                listing.Files.Add(filename);
            }

            return listing;
        }

        public static List<string> GetRootDirectoriesInDepot(string depotRootPath)
        {
            string workingDir = DepotPathUtilities.GetWorkingDirPath(depotRootPath);
            if (!Directory.Exists(workingDir))
                throw new Exception(workingDir + "does not exist");

            List<String> dirListing = new List<string>();

            foreach (string xmlFileName in Directory.GetFiles(workingDir, "*.xml"))
            {
                string rootDirectory = CMXmlUtilities.GetRootDirectoryFromXmlRootFile(xmlFileName);
                dirListing.Add(rootDirectory);
            }

            return dirListing;
        }

        public static long GetFileSize(string depotRoot, string hashedFilename)
        {
            string filePath = DepotPathUtilities.GetHashFilePath(depotRoot, hashedFilename);
            if (! File.Exists(filePath))
                throw new Exception(filePath + "does not exist");

            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;           
        }

        public static XDocument GetXml(string depotRoot, string hashedFilename)
        {
            string xmlFilePath = DepotPathUtilities.GetObjectFileXmlPath(depotRoot, hashedFilename);
            return XDocument.Load(xmlFilePath);
        }

        public static void UpdateXml(string depotRoot, string hashedFilename, XDocument fileXml)
        {
            string xmlFilePath = DepotPathUtilities.GetObjectFileXmlPath(depotRoot, hashedFilename);
            fileXml.Save(xmlFilePath);
        }
    }
}
