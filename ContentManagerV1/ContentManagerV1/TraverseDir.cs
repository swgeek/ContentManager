using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentManagerV1
{
    class TraverseDir
    {
        //private FileList mFileList;

        //public TraverseDir(string baseDirectory, FileList fileList)
        //{
        //    this.mFileList = fileList;
        //    TraverseDirRecursively(baseDirectory);
        //}

        //public void TraverseDirRecursively(string rootDirName)
        //{
        //    if (!Directory.Exists(rootDirName))
        //        throw new InvalidDataException();

        //    DirectoryInfo dirInfo = new DirectoryInfo(rootDirName);

        //    FileInfo[] filesInDir = dirInfo.GetFiles();
        //    foreach (FileInfo file in filesInDir)
        //        mFileList.AddFile(file);

        //    DirectoryInfo[] subdirectories = dirInfo.GetDirectories();
        //    foreach (DirectoryInfo dir in subdirectories)
        //    {
        //        TraverseDirRecursively(dir.FullName);
        //    }
        //}

        static public void TraverseDirRecursively(DirectoryInfo dirInfo, FileList fileList)
        {
            if (! dirInfo.Exists)
                throw new InvalidDataException();

            FileInfo[] filesInDir = dirInfo.GetFiles();
            foreach (FileInfo file in filesInDir)
                fileList.AddFile(file);

            DirectoryInfo[] subdirectories = dirInfo.GetDirectories();
            foreach (DirectoryInfo dir in subdirectories)
            {
                TraverseDirRecursively(dir, fileList);
            }

            fileList.AddDirectory(dirInfo);
        }

        static public FileList GetAllFilesInDir(DirectoryInfo dir)
        {
            FileList list = new FileList();

            TraverseDirRecursively(dir, list);

            return list;
        }
    }
}
