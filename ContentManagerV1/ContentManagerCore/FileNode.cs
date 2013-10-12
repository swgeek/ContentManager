using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentManagerCore
{
    // used when building up a directory tree, files are always a leaf.
    // may combine with directoryNode so have single node, but for now...
    public class FileNode
    {
        public string DepotName { get; private set; }
        public string HashValue { get; private set; }
        public List<string> OriginalPaths { get; set; }

        public FileNode(string depotName, string hashValue)
        {
            DepotName = depotName;
            HashValue = hashValue;
        }

        public void AddOriginalPath(string path)
        {
            OriginalPaths.Add(path);
        }
    }
}
