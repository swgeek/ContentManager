﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MpvUtilities
{
    public class FileList
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

