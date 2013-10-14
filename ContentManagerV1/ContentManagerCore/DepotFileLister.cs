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
        public static int ListAllFilesInDepot(string depotRootPath, string filelistDbRootPath)
        {
            string depotName = System.IO.Path.GetFileName(depotRootPath);
        
            int count = 0;
            string filesSubdirName = "files";
            string objectDirName = System.IO.Path.Combine(depotRootPath, filesSubdirName);
            if (!Directory.Exists(objectDirName))
                throw new Exception(objectDirName + " does not exist - did you give correct root of archive directory?");

            string currentDirectoryName = string.Empty;
            for (int i = 0x00; i < 0x100; i++)
            {
                List<string> filenameList = new List<string>();

                currentDirectoryName = System.IO.Path.Combine(objectDirName, i.ToString("X2"));
                if (Directory.Exists(currentDirectoryName))
                {
                    DirectoryInfo currentDirectory = new DirectoryInfo(currentDirectoryName);

                    foreach (FileInfo file in currentDirectory.GetFiles())
                    {
                        if (file.Extension != ".xml")
                        {
                            count++;
                            //string fileNameAndSize = file.Name; // just file name for old version files
                            string fileNameAndSize = file.Name + ";;" + file.Length.ToString() + ";;" + depotName;
                            filenameList.Add(fileNameAndSize);
                        }
                    }

                    if (filenameList.Count > 0)
                    {
                        string outputFileName = System.IO.Path.Combine(filelistDbRootPath, i.ToString("X2")) + ".txt";

                        if (File.Exists(outputFileName))
                        {
                            string[] existingFilenames = File.ReadAllLines(outputFileName);
                            filenameList.AddRange(existingFilenames);
                        }

                        File.WriteAllLines(outputFileName, filenameList.Distinct().ToArray());
                    }
                }
            }
            return count;
        }

        public static string[] GetListOfAllHashedFilesInDepot(string depotRootPath)
        {
            string depotName = System.IO.Path.GetFileName(depotRootPath);
            List<string> filelist = new List<string>();
            string objectDirName = MpvUtilities.DepotPathUtilities.GetObjectStoreDirNamePath(depotRootPath);

            if (!Directory.Exists(objectDirName))
                throw new Exception(objectDirName + " does not exist - did you give correct root of archive directory?");

            string currentDirectoryName = string.Empty;
            for (int i = 0x00; i < 0x100; i++)
            {
                currentDirectoryName = System.IO.Path.Combine(objectDirName, i.ToString("X2"));
                if (Directory.Exists(currentDirectoryName))
                {
                    DirectoryInfo currentDirectory = new DirectoryInfo(currentDirectoryName);

                    foreach (FileInfo file in currentDirectory.GetFiles())
                    {
                        if (file.Extension != ".xml")
                        {
                            filelist.Add(file.Name);
                        }
                    }
                }
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
            string workingDir = MpvUtilities.DepotPathUtilities.GetWorkingDirPath(depotRootPath);
            if (!Directory.Exists(workingDir))
                throw new Exception(workingDir + "does not exist");

            List<String> dirListing = new List<string>();

            foreach (string xmlFileName in Directory.GetFiles(workingDir, "*.xml"))
            {
                // for now just put xml filename in list
                string rootDirectory = CMXmlUtilities.GetRootDirectoryFromXmlRootFile(xmlFileName);
                dirListing.Add(rootDirectory);
            }

            return dirListing;
        }

        public static long GetFileSize(string depotRoot, string hashedFilename)
        {
            string filePath = MpvUtilities.DepotPathUtilities.GetHashFilePath(depotRoot, hashedFilename);
            if (! File.Exists(filePath))
                throw new Exception(filePath + "does not exist");

            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;           
        }

        public static XDocument GetXml(string depotRoot, string hashedFilename)
        {
            string xmlFilePath = MpvUtilities.DepotPathUtilities.GetObjectFileXmlPath(depotRoot, hashedFilename);
            return XDocument.Load(xmlFilePath);
        }

        public static void UpdateXml(string depotRoot, string hashedFilename, XDocument fileXml)
        {
            string xmlFilePath = MpvUtilities.DepotPathUtilities.GetObjectFileXmlPath(depotRoot, hashedFilename);
            fileXml.Save(xmlFilePath);
        }
    }
}
