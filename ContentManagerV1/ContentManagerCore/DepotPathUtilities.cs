using MpvUtilities;
using System;
using System.IO;

namespace ContentManagerCore
{
    public class DepotPathUtilities
    {
        const string workingDirName = "working";
        const string objectStoreDirName = "files";

        static public string GetDepotName(string depotRootPath)
        {
            // for now depot name is just name of root directory
            return System.IO.Path.GetFileName(depotRootPath);
        }

        static public string GetWorkingDirPath(string depotRoot)
        {
            return Path.Combine(depotRoot, workingDirName);
        }

        static public string GetObjectStoreDirNamePath(string depotRoot)
        {
            return Path.Combine(depotRoot, objectStoreDirName);
        }

        static public string GetHashFilePath(string depotRootPath, string hashValue)
        {
            string dirName = GetHashFileParentDirectoryPath(depotRootPath, hashValue);

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            return System.IO.Path.Combine(dirName, hashValue);
        }

        public static string GetExistingXmlDirectoryInfoFileName(string originalDirectoryPath, string depotRootPath)
        {
            string hashValue = SH1HashUtilities.HashString(originalDirectoryPath);

            string subDirName = hashValue.Substring(0, 2);
            string dirName = System.IO.Path.Combine(GetWorkingDirPath(depotRootPath), subDirName);

            if (!Directory.Exists(dirName))
                throw new Exception(dirName + "does not exist");

            string fullPath =  System.IO.Path.Combine(dirName, hashValue + ".xml");
            if (!File.Exists(fullPath))
                throw new Exception(fullPath + "does not exist!");

            return fullPath;
        }

        public static string GetXmlDirectoryInfoFileName(string originalDirectoryPath, string depotRootPath)
        {
            string hashValue = SH1HashUtilities.HashString(originalDirectoryPath);

            string subDirName = hashValue.Substring(0, 2);
            string dirName = System.IO.Path.Combine(GetWorkingDirPath(depotRootPath), subDirName);

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            return System.IO.Path.Combine(dirName, hashValue + ".xml");
        }

        public static string GetObjectFileXmlPath(string depotRoot, string hashFileName)
        {
            string dirPath = GetHashFileParentDirectoryPath(depotRoot, hashFileName);
            return System.IO.Path.Combine(dirPath, hashFileName + ".xml");
        }

        static public string GetHashFileParentDirectoryPath(string depotRootPath, string hashValue)
        {
            string subDirName = hashValue.Substring(0, 2);
            return System.IO.Path.Combine(GetObjectStoreDirNamePath(depotRootPath), subDirName);
        }

        static public string GetXmFileInfoPath(string parentDir, string filepath)
        {
            string filename = System.IO.Path.GetFileName(filepath);
            string subDirName = filename.Substring(0, 2);
            string subDirPath = System.IO.Path.Combine(parentDir, subDirName);
            if (!Directory.Exists(subDirPath))
            {
                Directory.CreateDirectory(subDirPath);
            }
            return System.IO.Path.Combine(subDirPath, filename);
        }
    }
}
