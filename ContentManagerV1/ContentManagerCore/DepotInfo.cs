using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Not sure if this works, maybe delete
// if keep, maybe roll into another project 
namespace ContentManagerCore
{
    public class DepotInfo
    {
        public string DepotRootPath { get; private set; }
        public string DepotName { get; private set; }
        public string[] RootDirectories { get; private set; }

        public DepotInfo(string depotRootPath)
        {
            DepotName = System.IO.Path.GetFileName(depotRootPath);
            RootDirectories = ContentManagerCore.DepotFileLister.GetRootDirectoriesInDepot(depotRootPath).ToArray();
        }
    }
}
