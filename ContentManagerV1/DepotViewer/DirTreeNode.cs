using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DepotViewer
{
    public class DirTreeNode
    {
        public string Name { get; set; }
        public string FullPath { get; set; }

        public List<DirTreeNode> SubDirs { get; set; }
        // probably replace file with some class so can have filesize etc.
        public List<string> Files { get; set; }

        public DirTreeNode()
        {
            SubDirs = new List<DirTreeNode>();
        }

        public DirTreeNode(string name, string fullpath)
        {
            Name = name;
            FullPath = fullpath;
        }

        public static DirTreeNode GetBaseDirs(string depotRootPath)
        {
            DirTreeNode rootDir = new DirTreeNode();

            foreach (string dirName in ContentManagerCore.DepotFileLister.GetRootDirectoriesInDepot(depotRootPath))
            {
                rootDir.SubDirs.Add(new DirTreeNode(dirName, dirName));
            }

            return rootDir;
        }

    }
}
