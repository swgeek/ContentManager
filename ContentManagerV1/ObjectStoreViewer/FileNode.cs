using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectStoreViewer
{
    public class FileNode
    {
        public string originalPath { get; set; }
        public string hashname { get; set; }
        public long fileSize { get; set; }
        // other options: list of all locations so can find dups from this
        // don't really need as can get from hashname
    }
}
