using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MpvUtilities
{
    public class DepotPathUtilities
    {
        const string workingDirName = "working";
        const string objectStoreDirName = "files";

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

            string subDirName = hashValue.Substring(0, 2);
            string dirName = System.IO.Path.Combine(GetObjectStoreDirNamePath(depotRootPath) , subDirName);

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            return System.IO.Path.Combine(dirName, hashValue);
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




    }
}
