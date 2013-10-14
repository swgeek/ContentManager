using System.IO;

namespace MpvUtilities
{
    public class TraverseDir
    {
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
