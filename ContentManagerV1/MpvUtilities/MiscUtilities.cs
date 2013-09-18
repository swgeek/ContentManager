using System;
using System.Collections.Generic;
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
    }
}
