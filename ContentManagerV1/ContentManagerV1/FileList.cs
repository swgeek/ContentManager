using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentManagerV1
{
    class FileList
    {
        public List<string> fileList {get; set;}

        public FileList()
        {
              fileList = new List<string>();
        }

        public int Count { get { return fileList.Count; } }

        public void AddFile(FileInfo newFile)
        {
            fileList.Add(newFile.FullName);
        }

        public void AddDirectory(DirectoryInfo dir)
        {
            // for now, just add to filelist
            fileList.Add(dir.FullName);
        }

        public string FileNames()
        {
            StringBuilder sb = new StringBuilder();

            foreach (string f in fileList)
            {
                sb.Append(f);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        public string CurrentFile()
        {
            if (this.Count > 0)
                return fileList.ElementAt(0);
            else
                return null;
        }

        public void RemoveCurrentFile()
        {
            fileList.RemoveAt(0);
        }

    }
}
