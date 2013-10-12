using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MpvUtilities;

namespace ContentManagerCore
{
    public class DirectoryNode
    {
        public string DepotName { get; private set; }
        public string HashValue { get; private set; }
        public string OriginalPath { get; private set; }

        public List<FileNode> Files { get; set; }
        public List<DirectoryNode> Directories { get; set; }

        public DirectoryNode(string depotName, string originalPath)
        {
            DepotName = depotName;
            OriginalPath = originalPath;
            HashValue = MpvUtilities.SH1HashUtilities.HashString(originalPath);
        }

        public void AddFile(FileNode newFile)
        {
            Files.Add(newFile);
        }

        public void AddDirectory(DirectoryNode newDir)
        {
            Directories.Add(newDir);
        }
    }
}
