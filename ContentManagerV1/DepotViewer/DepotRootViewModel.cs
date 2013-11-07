using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DepotViewer
{
    public class DepotRootViewModel
    {
        public string DepotName { get; private set; }
        public List<DirectoryNode> DirTrees { get; private set; }

        public DepotRootViewModel(string depotRootPath)
        {
            DepotName = Path.GetFileName(depotRootPath);
            DirTrees = new List<DirectoryNode>();

            foreach (string dir in ContentManagerCore.DepotFileLister.GetRootDirectoriesInDepot(depotRootPath))
            {
                DirectoryNode dirNode = new DirectoryNode(depotRootPath, dir, dir);
                dirNode.RecursivelyFill();
                DirTrees.Add(dirNode);
            }
        }
    }
}
