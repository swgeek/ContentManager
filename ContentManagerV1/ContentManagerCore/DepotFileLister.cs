using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentManagerCore
{

    public class DepotFileLister
    {
        public static int ListFiles(string depotRootPath, string filelistDbRootPath)
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
    }
}
