using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MpvUtilities
{
    public class MiscUtilities
    {

        public static string GetExistingHashFileName(string baseDir, string hashString, string extension)
        {
            string subDirName = hashString.Substring(0, 2);
            string fullPath = System.IO.Path.Combine(baseDir, subDirName, hashString) + extension;

            if (!System.IO.File.Exists(fullPath))
                throw new Exception(fullPath + " does not exist");

               
            return fullPath;
        }

        public static string GetOrCreateDirectoryForHashName(string hashName, string rootDirectory)
        {
            string firstTwoCharsOfHash = hashName.Substring(0, 2);
            string dirPath = Path.Combine(rootDirectory, firstTwoCharsOfHash);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            return dirPath;
        }

    }
}
