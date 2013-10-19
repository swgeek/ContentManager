using ContentManagerCore;
using System.Collections.Generic;
using System.IO;

namespace DepotViewer
{
    public class DirectoryNode
    {
        public string HashValue { get; private set; }
        public string OriginalPath { get; private set; }
        public string DepotRoot { get; set; }
        public string Name { get; set; }

        public List<FileNode> Files { get; set; }
        public List<DirectoryNode> Directories { get; set; }

        public DirectoryNode(string depotRoot, string originalPath)
        {
            DepotRoot = depotRoot;
            OriginalPath = originalPath;
            Name = Path.GetFileName(originalPath);
            HashValue = MpvUtilities.SH1HashUtilities.HashString(originalPath);
            Files = new List<FileNode>();
            Directories = new List<DirectoryNode>();
        }

        public void RecursivelyFill()
        {
            // add Directories
            DirListing listing = ContentManagerCore.DepotFileLister.GetDirListing(OriginalPath, DepotRoot);
            foreach (string dir in listing.Directories)
            {
                string newPath = Path.Combine(OriginalPath, dir);
                DirectoryNode newNode = new DirectoryNode(DepotRoot, newPath);
                Directories.Add(newNode);
            }

            foreach (string file in listing.Files)
            {
                FileNode newNode = new FileNode(DepotRoot, file);
                Files.Add(newNode);
            }

            // call recursively
            foreach (DirectoryNode dir in Directories)
                dir.RecursivelyFill();
        }
    }
}
