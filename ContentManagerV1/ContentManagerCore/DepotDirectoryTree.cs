using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentManagerCore
{

    // some overlap between this and DepotFileLister - maybe combine the two.


    class DepotDirectoryTree
    {
        public string DepotName { get; set; }
        public string DepotRootPath { get; set; }
        public string OriginalRootPath { get; set; }
        public DirectoryNode TreeRoot { get; private set; }

        // want to automatically fill out tree at construct time, but should I?
        // also, there are multiple roots. Should I fill them all?
        // Or maybe punt for now. Only create trees for a particular path, specified in constructer. I don't like this, prefer to point at root and everything
        // is filled out. What to do?
        public DepotDirectoryTree(string depotRootPath)
        {
            DepotRootPath = depotRootPath;
        }
    }
}
