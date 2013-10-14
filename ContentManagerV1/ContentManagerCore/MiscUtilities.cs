using MpvUtilities;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ContentManagerCore
{
    public class MiscUtilities
    {
        public static string GetOrCreateDirectoryForHashName(string hashName, string rootDirectory)
        {
            string firstTwoCharsOfHash = hashName.Substring(0, 2);
            string dirPath = Path.Combine(rootDirectory, firstTwoCharsOfHash);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            return dirPath;
        }

        public static bool CheckIfFileInOtherArchiveDb(string hashValue, string otherArchiveDbDirName)
        {
            if (otherArchiveDbDirName == string.Empty)
                return false;

            string dbFileName = hashValue.Substring(0, 2) + ".txt";
            string dbFileFullPath = System.IO.Path.Combine(otherArchiveDbDirName, dbFileName);
            if (!File.Exists(dbFileFullPath))
                return false;

            string[] archivedFileList = File.ReadAllLines(dbFileFullPath);

            if (archivedFileList.Contains(hashValue))
                return true;
            else
                return false;
        }


        public static void SetupForHashDirectory(string depotRootDirName, string sourceDirName)
        {
            string filesSubdirName = "files";
            string objectDestDirName = System.IO.Path.Combine(depotRootDirName, filesSubdirName);
            if (!Directory.Exists(objectDestDirName))
                Directory.CreateDirectory(objectDestDirName);

            string workingSubDirName = "working";
            string workingDestDirName = System.IO.Path.Combine(depotRootDirName, workingSubDirName);
            if (!Directory.Exists(workingDestDirName))
                Directory.CreateDirectory(workingDestDirName);

            XDocument rootDirXmlDoc = CMXmlUtilities.GenerateRootDirInfoDocument(sourceDirName);
            string hashValue = SH1HashUtilities.HashString(sourceDirName);
            string rootDirXmlPath = System.IO.Path.Combine(workingDestDirName, hashValue + ".xml");
            rootDirXmlDoc.Save(rootDirXmlPath);

        }

        public static void Log(string logText, string depotRootPath)
        {
            string logDir = DepotPathUtilities.GetWorkingDirPath(depotRootPath);
            string logFile = Path.Combine(logDir, "log.txt");
            File.WriteAllText(logFile, logText);
        }


        public static void AppendToLog(string logText, string depotRootPath)
        {
            string logDir = DepotPathUtilities.GetWorkingDirPath(depotRootPath);
            string logFile = Path.Combine(logDir, "log.txt");
            string logContents = File.ReadAllText(logFile);

            File.WriteAllText(logFile, logContents + logText);
        }

    }
}
