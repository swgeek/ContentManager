using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectStoreViewer
{
    public class DirNode
    {
        public string name { get; set; }
        public string fullPath { get; set; }
        public string hash { get; set; }
        public List<DirNode> subdirectories = new List<DirNode>();
        public List<FileNode> files = new List<FileNode>();
    }
}
